module TaskMan.API.Program

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Options

open FSharp.Data.Sql

open Giraffe
open Serilog

open TaskMan.Core.Database
open TaskMan.Core.Types
open TaskMan.Core.Interfaces

open TaskMan.API.Http.Routes
open TaskMan.API.Store
open TaskMan.API.Messaging
open TaskMan.API.Messaging.Publisher
open TaskMan.API.Messaging.ConsumerDaemon
open Microsoft.Extensions.Configuration

// ---------------------------------
// Error handler
// ---------------------------------
let errorHandler (ex : Exception) (logger : Microsoft.Extensions.Logging.ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text "An unhandled error occured."

// --------------------------------- 
// Config Helpers 
// ---------------------------------

type LocalWebHostBuilder =
    { Builder: IWebHostBuilder
      ConfigureFn: IApplicationBuilder -> IApplicationBuilder }

let withLocalBuilder builder = { Builder = builder; ConfigureFn = id }

let configureAppConfiguration fn (builder: LocalWebHostBuilder) =
    let bldr =
        builder.Builder.ConfigureAppConfiguration(
            Action<WebHostBuilderContext, IConfigurationBuilder>(fun ctx bldr -> fn ctx bldr |> ignore)
        )

    { builder with Builder = bldr }

let configureServices fn (builder: LocalWebHostBuilder) =
    let bldr =
        builder.Builder.ConfigureServices(
            Action<WebHostBuilderContext, IServiceCollection>(fun ctx svc -> fn ctx svc |> ignore)
        )

    { builder with Builder = bldr }

let configure (fn: IApplicationBuilder -> IApplicationBuilder) (builder: LocalWebHostBuilder) =
    let cfgFn = builder.ConfigureFn >> fn

    let bldr =
        builder.Builder.Configure(Action<IApplicationBuilder>(fun app -> cfgFn app |> ignore))

    { Builder = bldr; ConfigureFn = cfgFn }

let build (bldr: WebApplicationBuilder) =
    bldr.Build()

let run (app: WebApplication) =
    app.Run()

// ---------------------------------
// Config and Main
// ---------------------------------

let withConfiguration (bldr: LocalWebHostBuilder) =
    bldr
    |> configureAppConfiguration (fun context config ->
        config
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true)
            .AddEnvironmentVariables()
        |> ignore)
        
let withSerilogRequestLogging (bldr: LocalWebHostBuilder) =
    bldr
    |> configure (fun app -> app.UseSerilogRequestLogging())

let configureSerilog
    (context : HostBuilderContext)
    (config: LoggerConfiguration)
    =
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
    |> ignore

let addLogging (bldr: WebApplicationBuilder) =
    bldr.Logging
        .AddConsole()
        .AddDebug()
        |> ignore
    bldr

let addSerilog (bldr: WebApplicationBuilder): WebApplicationBuilder =
    configureSerilog |> bldr.Host.UseSerilog |> ignore
    bldr |> addLogging

let addGiraffe (bldr: WebApplicationBuilder) =
    bldr.Services.AddGiraffe() |> ignore
    bldr

let withGiraffe bldr =
    bldr
    |> configureServices (fun _ services -> services.AddGiraffe())
    |> configure (fun app ->
        let config = app.ApplicationServices.GetService<IOptions<RootConfig>>().Value.TaskMan
        let env = app.ApplicationServices.GetService<IWebHostEnvironment>()

        if not (env.IsDevelopment()) then
            app.UseGiraffeErrorHandler(errorHandler) |> ignore

        app.UseGiraffe(webApp config.Token)
        app)

let addCors (bldr: WebApplicationBuilder) =
    bldr.Services.AddCors() |> ignore
    bldr

let configureCors (bldr : CorsPolicyBuilder) =
    bldr
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
        |> ignore

let useCors (app: WebApplication) =
    app.UseCors(configureCors) |> ignore
    app

let withServices bldr =
    bldr |> configureServices (fun context services ->
        services
            .Configure<RootConfig>(context.Configuration)
            .AddScoped<TaskDb.dataContext>(fun provider ->
                let config = provider.GetRequiredService<IOptions<RootConfig>>()
                TaskDb.GetDataContext(config.Value.Database.ConnectionString, selectOperations = SelectOperations.DatabaseSide)
            )
            .AddSingleton<ConnectionStore>(fun provider ->
                let config = provider.GetRequiredService<IOptions<RootConfig>>()
                new ConnectionStore(config.Value.RabbitMQConnection))
            .AddScoped<Publisher>(fun provider ->
                let config = provider.GetRequiredService<IOptions<RootConfig>>()
                let conn = provider.GetRequiredService<ConnectionStore>().GetDefaultConnection()
                let logger = provider.GetRequiredService<Serilog.ILogger>()
                new Publisher(conn, logger, config.Value))
            .AddHostedService<ConsumerDaemon>()
            .AddScoped<ITaskStore,TaskStore>() |> ignore   
            )

let useSerilogRequestLogging (app: WebApplication) =
    app.UseSerilogRequestLogging() |> ignore
    app

///////////////////////////////////////////////////////////////
// For saving the schema
// let ctx = TaskDb.GetDataContext()
// ctx.``Design Time Commands``.SaveContextSchema |> ignore
//
///////////////////////////////////////////////////////////////

[<EntryPoint>]
let main args =
    async {
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                |> withLocalBuilder
                |> withConfiguration
                |> withSerilogRequestLogging
                |> withGiraffe
                |> withServices
                |> ignore
                )
            .UseSerilog(configureSerilog)
            .Build()
            .Run()

        return 0
    } |> Async.RunSynchronously
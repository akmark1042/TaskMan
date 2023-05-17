module TaskMan.API.Messaging.ConsumerDaemon

open System
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open Microsoft.Extensions.Hosting

open TaskMan.Core.Types
open Consumer

type internal ConsumerDaemon (provider:IServiceProvider, logger:Serilog.ILogger) =
    inherit BackgroundService()
    let defaultConnection = provider.GetRequiredService<ConnectionStore>().GetDefaultConnection()
    let config = provider.GetRequiredService<IOptions<RootConfig>>().Value

    let init (cancellationToken:CancellationToken) : Async<unit> = 
        async {
            try
                let channel = defaultConnection.CreateModel()

                channel.ExchangeDeclare(
                        exchange = config.Exchange,
                        ``type`` = RabbitMQ.Client.ExchangeType.Topic.ToString(),
                        durable = true,
                        autoDelete = false,
                        arguments = null
                    )

                logger.Information("Exchange Declared")

                channel.QueueDeclare(
                    queue = config.Queue,
                    durable = true,
                    exclusive = false,
                    autoDelete = false,
                    arguments = null
                ) |> ignore

                channel.QueueBind(
                    config.Queue,
                    config.Exchange,
                    "#",
                    null
                )

                logger.Information("Queue declared and bound")
                
                let consumer = Consumer(channel, logger, provider)
                
                let consumerTag = "consumertag1"

                channel.BasicConsume(
                    queue = config.Queue,
                    autoAck = false,
                    consumerTag = consumerTag,
                    noLocal = true,
                    exclusive = false,
                    arguments = null,
                    consumer = consumer
                ) |> ignore

                logger.Information("Consumer basic complete")
            with
            | er -> printfn "An error occured: %A" er
        }
    
    override this.ExecuteAsync(cancellationToken:CancellationToken) : Tasks.Task =
        Async.StartAsTask(init cancellationToken)
module TaskMan.API.Messaging.Consumer

open System
open System.Text.Json
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options

open RabbitMQ.Client

open TaskMan.Core.Types
open TaskMan.Core

let handleEvent<'T>
    (logger: Serilog.ILogger)
    (channel: IModel)
    (routingKey: string)
    (redelivered: bool)
    (deliveryTag: uint64)
    (payload: Result<'T, string>)
    (action: 'T -> bool -> unit) =
        logger.Information(sprintf "Del tag: %i" deliveryTag)
        match payload with
        | Ok event ->
            logger.Debug(sprintf "Handling internal event with routing key %s: %O" routingKey event)
            action event redelivered
            logger.Information("Action completed")
            // if handling event doesn't throw, acknowledge message
            channel.BasicAck(deliveryTag, false)
            logger.Information("Ack")
        | Error err ->
            logger.Error(err)
            // if event is invalid allow to retry once in case of message corruption
            channel.BasicReject(deliveryTag, not redelivered)

type Consumer(channel:IModel, logger: Serilog.ILogger, provider:IServiceProvider) =

    inherit DefaultBasicConsumer(channel)

    let config = provider.GetRequiredService<IOptions<RootConfig>>().Value
    
    member this.HandleEvent<'T>
        (routingKey:string)
        (redelivered: bool)
        (deliveryTag: uint64)
        (payload:Result<'T, string>)
        (action: 'T -> bool -> unit)
        = handleEvent<'T> logger channel routingKey redelivered deliveryTag payload action

    override this.HandleBasicDeliver
        ( _consumerTag: string
        , deliveryTag: uint64
        , redelivered: bool
        , _exchange: string
        , routingKey: string
        , _properties: IBasicProperties
        , body: ReadOnlyMemory<byte>
        ) =
        try
            use scope = provider.CreateScope()
            let store = scope.ServiceProvider.GetRequiredService<ITaskStore>()

            match routingKey with
            | DELETE_TASK_ROUTING_KEY ->
                let dto:Result<DeleteTaskDTO, string> =
                    try
                        logger.Information("deserializing")
                        JsonSerializer.Deserialize<DeleteTaskDTO>(body.ToArray()) |> Ok
                    with
                        | _ -> Error "Could not deserialize the event."

                logger.Information($"DTO serialized successfully: %b{dto |> Result.isOk}" )
                this.HandleEvent routingKey redelivered deliveryTag dto (fun x _ -> store.deleteTaskAsync x.Task_Name |> Async.RunSynchronously |> ignore)
            
            | UPDATE_TASK_ROUTING_KEY ->
                let dto:Result<UpdateTaskStatusDTO, string> =
                    try
                        logger.Information("deserializing")
                        JsonSerializer.Deserialize<UpdateTaskStatusDTO>(body.ToArray()) |> Ok
                    with
                        | _ -> Error "Could not deserialize the event."

                logger.Information($"DTO serialized successfully: %b{dto |> Result.isOk}" )
                this.HandleEvent routingKey redelivered deliveryTag dto (fun x _ -> store.finishTaskAsync x.Id |> Async.RunSynchronously |> ignore)

            | ADD_TASK_ROUTING_KEY ->
                let dto =
                    try
                        logger.Information("deserializing")
                        JsonSerializer.Deserialize<CreateTask>(body.ToArray()) |> Ok
                    with
                        | _ -> Error "Could not deserialize the event."
                    
                logger.Information($"DTO serialized successfully: %b{dto |> Result.isOk}" )
                
                let newSub:Result<CreateTask, string> = 
                    dto |> Result.map (fun x -> {
                        Task_Name = x.Task_Name
                        Type = x.Type
                        Status = x.Status
                        Created_on = x.Created_on
                        Created_by = x.Created_by
                        Last_updated = x.Last_updated
                        Updated_by = x.Updated_by
                    })
                
                this.HandleEvent routingKey redelivered deliveryTag newSub (fun x _bool -> store.addTaskAsync x |> Async.RunSynchronously |> ignore)
            
            | _ -> channel.BasicReject(deliveryTag, false)
        with
            | _ -> channel.BasicReject(deliveryTag, true)
            
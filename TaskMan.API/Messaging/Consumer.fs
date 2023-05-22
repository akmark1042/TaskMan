module TaskMan.API.Messaging.Consumer

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options

open RabbitMQ.Client

open MTA.Messaging.Client.ProtoBufNet

open TaskMan.Core.Types
open TaskMan.Core
open TaskMan.Protobuf
open TaskMan.API.Messaging.Types

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
            | UPDATE_TASK_ROUTING_KEY ->
                let dto =
                    deserializeEvent<UpdateTaskStatusEventProtoDTO>(body) |> UpdateTaskStatusEventDTO.ofProtobuf
                
                logger.Information($"DTO serialized successfully: %O{dto}" )
                this.HandleEvent routingKey redelivered deliveryTag ((UpdateTaskStatusEventDTO.toDomain dto dto.Updated_by) |> Ok) (fun x _bool -> store.updateStatusAsync x.Id x |> Async.RunSynchronously)

            | DELETE_TASK_ROUTING_KEY ->
                let dto =
                    deserializeEvent<DeleteTaskEventProtoDTO>(body) |> DeleteTaskEventDTO.ofProtobuf

                logger.Information($"DTO serialized successfully: %O{dto}" )
                this.HandleEvent routingKey redelivered deliveryTag (dto |> DeleteTaskEventDTO.toDomain |> Ok) (fun x _bool -> store.deleteTaskAsync x.Id |> Async.RunSynchronously |> ignore)
            
            | ADD_TASK_ROUTING_KEY ->
                let dto =
                    deserializeEvent<CreateTaskEventProtoDTO>(body) |> CreateTaskEventDTO.ofProtobuf
                    
                logger.Information($"DTO serialized successfully: %O{dto}" )
                this.HandleEvent routingKey redelivered deliveryTag (dto |> CreateTaskEventDTO.toDomain |> Ok) (fun x _bool -> store.addTaskAsync x |> Async.RunSynchronously |> ignore)

            | _ -> channel.BasicReject(deliveryTag, false)
        with
            | _ -> channel.BasicReject(deliveryTag, true)
            
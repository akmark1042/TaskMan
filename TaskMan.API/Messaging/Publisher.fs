module TaskMan.API.Messaging.Publisher

open System
open System.Text.Json

open RabbitMQ.Client

open MTA.Messaging.Client.ProtoBufNet

open TaskMan.Core.Types
open TaskMan.API.Messaging.Types

type Publisher (connection:IConnection, logger: Serilog.ILogger, config:RootConfig) =
    let mutable channel = connection.CreateModel()
    let mutable disposed = false

    member this.DispatchAddSubscriptionEvent(event:CreateTaskEvent) =
        //let body = JsonSerializer.SerializeToUtf8Bytes event |> ReadOnlyMemory

        let body = event |> CreateTaskEventDTO.fromDomain |> CreateTaskEventDTO.toProtobuf |> toReadOnlyMemory
        let props = channel.CreateBasicProperties()
        props.Persistent <- true
        
        channel.BasicPublish(
            exchange = config.Exchange,
            routingKey = ADD_TASK_ROUTING_KEY,
            basicProperties = props,
            body = body
        )

    member this.DispatchDeleteTaskEvent(event:DeleteTaskEvent) =
        let body = event |> DeleteTaskEventDTO.fromDomain |> DeleteTaskEventDTO.toProtobuf |> toReadOnlyMemory
        let props = channel.CreateBasicProperties()
        props.Persistent <- true
        
        channel.BasicPublish(
            exchange = config.Exchange,
            routingKey = DELETE_TASK_ROUTING_KEY,
            basicProperties = props,
            body = body
        )

    member this.DispatchUpdateSubscriptionEvent(event:UpdateTaskStatusEvent) =
        let body = event |> UpdateTaskStatusEventDTO.fromDomain |> UpdateTaskStatusEventDTO.toProtobuf |> toReadOnlyMemory
        let props = channel.CreateBasicProperties()
        props.Persistent <- true
        
        channel.BasicPublish(
            exchange = config.Exchange,
            routingKey = UPDATE_TASK_ROUTING_KEY,
            basicProperties = props,
            body = body
        )
   
    member this.Dispose(disposing:bool) =
        if not disposed then    
            if disposing then
                channel.Dispose()
                channel <- null
            disposed <- true
    
    interface IDisposable with
        member this.Dispose() =
            this.Dispose(true)
            GC.SuppressFinalize(this)
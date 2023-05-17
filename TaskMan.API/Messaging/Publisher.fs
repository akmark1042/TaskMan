module TaskMan.API.Messaging.Publisher

open System
open System.Text.Json

open RabbitMQ.Client

open TaskMan.Core.Types

type Publisher (connection:IConnection, logger: Serilog.ILogger, config:RootConfig) =
    let mutable channel = connection.CreateModel()
    let mutable disposed = false

    member this.DispatchDeleteTaskEvent(event:DeleteTaskDTO) =
        let body = JsonSerializer.SerializeToUtf8Bytes event |> ReadOnlyMemory
        let props = channel.CreateBasicProperties()
        props.Persistent <- true
        
        channel.BasicPublish(
            exchange = config.Exchange,
            routingKey = DELETE_TASK_ROUTING_KEY,
            basicProperties = props,
            body = body
        )

    member this.DispatchUpdateSubscriptionEvent(event:UpdateTaskStatusDTO) =
        let body = JsonSerializer.SerializeToUtf8Bytes event |> ReadOnlyMemory
        let props = channel.CreateBasicProperties()
        props.Persistent <- true
        
        channel.BasicPublish(
            exchange = config.Exchange,
            routingKey = UPDATE_TASK_ROUTING_KEY,
            basicProperties = props,
            body = body
        )

    member this.DispatchAddSubscriptionEvent(event:CreateTask) =
        let body = JsonSerializer.SerializeToUtf8Bytes event |> ReadOnlyMemory
        let props = channel.CreateBasicProperties()
        props.Persistent <- true
        
        channel.BasicPublish(
            exchange = config.Exchange,
            routingKey = ADD_TASK_ROUTING_KEY,
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
[<AutoOpen>]
module TaskMan.API.Messaging.ConnectionStore

open RabbitMQ.Client

open TaskMan.Core.Types
open System

type ConnectionStore (rmqConfig:RabbitMQConfig) =
    let mutable disposed = false

    let BuildConnection (config:RabbitMQConfig) =
        let factory = ConnectionFactory()
        factory.VirtualHost <- config.VirtualHost
        factory.UserName <- config.Username
        factory.Password <- config.Password
        let endpoints =
            config.Hosts
            |> Seq.map
                (fun x -> 
                    let h, p =
                        match x.Split ":" |> List.ofArray with
                        | [h] -> h, 5672
                        | [h; p] -> h, int p
                        | _ -> failwith "Invalid RabbitMQ host format"
                    AmqpTcpEndpoint(h, p, SslOption(config.ClusterFQDN))
                ) |> Seq.toArray
        
        factory.CreateConnection endpoints

    let defaultConnection = Lazy<IConnection>(fun _ -> BuildConnection rmqConfig)
    
    member this.GetDefaultConnection() = defaultConnection.Value

    member this.Dispose(disposing:bool) =
        if not disposed then
            if disposing then
                if defaultConnection.IsValueCreated then
                    defaultConnection.Value.Close()
                    defaultConnection.Value.Dispose()
                disposed <- true
    
    interface IDisposable with
        member this.Dispose() =
            this.Dispose(true)
            GC.SuppressFinalize(this)
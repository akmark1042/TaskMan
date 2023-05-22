module TaskMan.API.Messaging.ConsumerDaemon

open System
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open Microsoft.Extensions.Hosting

open MTA.Messaging.Client.RabbitMQ.Common
open MTA.Messaging.Client.RabbitMQ.Consumer

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

                let exchangeConfig = {
                    defaultExchangeConfig config.Exchange with Type = ExchangeType.Topic
                }

                createExchange channel exchangeConfig

                logger.Information("Exchange Declared")

                let consumerConfig = defaultConsumerConfig config.Exchange config.Queue
                let consumerChannel = defaultConnection.CreateModel()

                logger.Information("Queue declared and bound")

                use handle = Consumer(consumerChannel, logger, provider) |> startConsumer consumerChannel consumerConfig
                do! Async.AwaitWaitHandle cancellationToken.WaitHandle |> Async.Ignore
                handle.Close()                

                logger.Information("Consumer basic complete")
            with
            | er -> printfn "An error occured: %A" er
        }
    
    override this.ExecuteAsync(cancellationToken:CancellationToken) : Tasks.Task =
        Async.StartAsTask(init cancellationToken)
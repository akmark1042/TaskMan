[<AutoOpen>]
module TaskMan.Core.Database

open FSharp.Data.Sql

open TaskMan.Core.Types

[<Literal>]
let dbVendor = Common.DatabaseProviderTypes.POSTGRESQL

[<Literal>]
let connString = "Host=localhost;Database=taskman;User ID=taskman;Password=password;"

[<Literal>]
let contextSchemaPath = __SOURCE_DIRECTORY__ + "/database.schema"

[<Literal>]
let useOptionTypes = Common.NullableColumnType.OPTION

type TaskDb = SqlDataProvider<
    DatabaseVendor = dbVendor,
    ConnectionString = connString,
    UseOptionTypes = useOptionTypes,
    ContextSchemaPath = contextSchemaPath >

type TaskDatabase (config:DatabaseConfig) =
    let dataContext : TaskDb.dataContext =
        TaskDb.GetDataContext(config.ConnectionString)

    member this.getDataContext() = dataContext

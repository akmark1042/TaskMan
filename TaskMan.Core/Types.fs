module TaskMan.Core.Types

open System

[<CLIMutable>]
type DatabaseConfig =
    {
        ConnectionString : string
    }

[<CLIMutable>]
type RabbitMQConfig =
    {
        Hosts: string seq
        ClusterFQDN: string
        VirtualHost: string
        SSL: bool
        Username: string
        Password: string
    }

[<CLIMutable>]
type TaskManConfig =
    {
        Token: string
    }

[<CLIMutable>]
type RootConfig =
    {
        Database : DatabaseConfig
        TaskMan : TaskManConfig
        RabbitMQConnection : RabbitMQConfig
        Exchange: string
        Queue: string
    }

type Task =
    {
        Id: int
        Task_Name: string
        Type: string option
        Status: int
        Created_on: DateTimeOffset
        Created_by: string
        Last_updated: DateTimeOffset
        Updated_by: string
    }

module Task =
    let toDTO (item:Task) : Task =
        {
            Id = item.Id
            Task_Name = item.Task_Name
            Type = item.Type
            Status = item.Status
            Created_on = item.Created_on
            Created_by = item.Created_by
            Last_updated = item.Last_updated
            Updated_by = item.Updated_by
        }

type CreateTask =
    {
        Task_Name: string
        Type: string
        Status: int
        Created_on: DateTimeOffset
        Created_by: string
        Last_updated: DateTimeOffset
        Updated_by: string
        
    }

type TaskDTO =
    {
        Id: int
        Task_Name: string
        Type: string
        Status: int
        Created_on: DateTimeOffset
        Created_by: string
        Last_updated: DateTimeOffset
        Updated_by: string
    }

module TaskDTO =
    let toTask (item:TaskDTO) : Task =
        {
            Id = item.Id
            Task_Name = item.Task_Name
            Type = Some item.Type
            Status = item.Status
            Created_on = item.Created_on
            Created_by = item.Created_by
            Last_updated = item.Last_updated
            Updated_by = item.Updated_by
        }
    
    let toCreate (item:TaskDTO) : CreateTask =
        {
            Task_Name = item.Task_Name
            Type = item.Type
            Status = item.Status
            Created_on = item.Created_on
            Created_by = item.Created_by
            Last_updated = item.Last_updated
            Updated_by = item.Updated_by
        }

[<Literal>]
let ADD_TASK_ROUTING_KEY = "add.task"

[<Literal>]
let DELETE_TASK_ROUTING_KEY = "delete.task"

[<Literal>]
let UPDATE_TASK_ROUTING_KEY = "update.task"

type Status =
   | New = 0
   | Received = 1 //Reviewed
   | Idle = 2
   | InProgress = 3
   | Halted = 4 //error status, did not complete its run or was cancelled
   | Complete = 5
   | Deprecated = 6

type DeleteTaskDTO =
    {
        Task_Name: string
    }

type UpdateTaskStatusDTO =
    {
        Id: int
    }

type AuthToken = AuthToken of String

module AuthToken =
    let unwrap (AuthToken token) = token

    let validate (headers:string list) =
        match headers with
        | [] -> Error "No headers provided."
        | (value::_) -> Ok value
    
    let getToken (header:string) =
        if header.StartsWith("Basic ")
        then header.Replace("Basic ", "") |> Ok
        else Error "Not a basic authentication token."
    
    let make (authorization:string list) =
        authorization |> validate |> Result.bind getToken |> Result.map AuthToken
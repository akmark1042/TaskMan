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
    let ofTask (item:Task) : Task =
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
    
    let toTaskDTO (item:Task) : TaskDTO =
        {
            Id = item.Id
            Task_Name = item.Task_Name
            Type = (Option.defaultValue "" item.Type)
            Status = item.Status
            Created_on = item.Created_on
            Created_by = item.Created_by
            Last_updated = item.Last_updated
            Updated_by = item.Updated_by
        }

type TaskType =
    Default
    | UserGenerated
    | Admin
    | Update
    | Unknown

type CreateTask =
    {
        Task_Name: string
        Type: TaskType
        Status: int
        Created_by: string
    }

module TaskType =
    let parse (str:string) =
        match str.ToLower() with
        | "default" -> Default
        | "user_generated" -> UserGenerated
        | "admin" -> Admin
        | "update" -> Update
        | _ -> Unknown

type Status =
   | New = 0
   | Received = 1 //Reviewed
   | Idle = 2
   | InProgress = 3
   | Halted = 4 //error status, did not complete its run or was cancelled
   | Complete = 5
   | Deprecated = 6

type CreateTaskEvent =
    {
        Task_Name: string
        Type: TaskType
        Status: int
        Created_by: string
    }

type UpdateTaskStatusEvent =
    {
        Id: int
        Type: string
        Status: int
        Updated_by: string
    }

type DeleteTaskEvent =
    {
        Id: int
    }
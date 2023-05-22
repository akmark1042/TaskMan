module TaskMan.API.Messaging.Types

open System

open TaskMan.Core.Types
open TaskMan.Protobuf

[<Literal>]
let ADD_TASK_ROUTING_KEY = "add.task"

[<Literal>]
let DELETE_TASK_ROUTING_KEY = "delete.task"

[<Literal>]
let UPDATE_TASK_ROUTING_KEY = "update.task"

type CreateTaskEventDTO =
    {
        Task_Name: string
        Type: string
        Status: int
        Created_by: string
    }

module CreateTaskEventDTO =
    let toDomain (dto: CreateTaskEventDTO) : CreateTaskEvent =
        {
            Task_Name = dto.Task_Name
            Type = dto.Type |> TaskType.parse
            Status = dto.Status
            Created_by =dto.Created_by
        }

    let fromDomain (ev: CreateTaskEvent) : CreateTaskEventDTO =
        {
            Task_Name = ev.Task_Name
            Type = ev.Type.ToString()
            Status = ev.Status
            Created_by = ev.Created_by
        }

    let toProtobuf (dto:CreateTaskEventDTO) : CreateTaskEventProtoDTO =
        let proto = CreateTaskEventProtoDTO()
        proto.TaskName <- dto.Task_Name
        proto.Type <- dto.Type
        proto.Status <- dto.Status
        proto.CreatedBy <- dto.Created_by
        proto
    
    let ofProtobuf (proto:CreateTaskEventProtoDTO) : CreateTaskEventDTO =
        {
            Task_Name = proto.TaskName
            Type = proto.Type.ToString()
            Status = proto.Status
            Created_by = proto.CreatedBy
        }

type CreateTaskDTO =
    {
        Task_Name: string
        Type: string
        Status: int
    }

module CreateTaskDTO =
    let toEvent (item:CreateTaskDTO) (creator:string) : CreateTaskEvent =
        {
            Task_Name = item.Task_Name
            Type = TaskType.parse item.Type
            Status = item.Status
            Created_by = creator
        }

type UpdateTaskStatusEventDTO =
    {
        Id: int
        Type: string
        Status: int
        Updated_by: string
    }

module UpdateTaskStatusEventDTO =
    let toDomain (dto: UpdateTaskStatusEventDTO) (updater:string) : UpdateTaskStatusEvent =
        {
            Id = dto.Id
            Type = dto.Type
            Status = dto.Status
            Updated_by = updater
        }

    let fromDomain (ev: UpdateTaskStatusEvent) : UpdateTaskStatusEventDTO =
        {
            Id = ev.Id
            Type = ev.Type.ToString()
            Status = ev.Status
            Updated_by = ev.Updated_by
        }

    let toProtobuf (dto:UpdateTaskStatusEventDTO) : UpdateTaskStatusEventProtoDTO =
            let proto = UpdateTaskStatusEventProtoDTO()
            proto.Id <- dto.Id
            proto.Type <- dto.Type
            proto.Status <- dto.Status
            proto.UpdatedBy <- dto.Updated_by
            proto

    let ofProtobuf (proto:UpdateTaskStatusEventProtoDTO) : UpdateTaskStatusEventDTO =
        {
            Id = proto.Id
            Type = proto.Type.ToString()
            Status = proto.Status
            Updated_by = proto.UpdatedBy
        }

type DeleteTaskEventDTO =
    {
        Id: int
    }

module DeleteTaskEventDTO =
    let toDomain (dto: DeleteTaskEventDTO) : DeleteTaskEvent =
        {
            Id = dto.Id
        }

    let fromDomain (ev: DeleteTaskEvent) : DeleteTaskEventDTO =
        {
            Id = ev.Id
        }

    let toProtobuf (dto:DeleteTaskEventDTO) : DeleteTaskEventProtoDTO =
        let proto = DeleteTaskEventProtoDTO() 
        proto.Id <- dto.Id
        proto
    
    let ofProtobuf (proto:DeleteTaskEventProtoDTO) : DeleteTaskEventDTO =
        {
            Id = proto.Id
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
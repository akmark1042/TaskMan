module TaskMan.API.Http.Handlers

open System
open Microsoft.AspNetCore.Http

open Giraffe

open TaskMan.Core.Interfaces
open TaskMan.Core.Default
open TaskMan.Core.Types
open TaskMan.API.Messaging.Publisher
open TaskMan.API.Messaging.Types

let handleGetAllTasks =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let store = ctx.GetService<ITaskStore>()
        let! result = GetAllTasksAsync store

        ctx.SetStatusCode 200
        return! json result next ctx
    }

let handleGetTaskById idx =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let store: ITaskStore = ctx.GetService<ITaskStore>()
        let! mItem = GetTaskByIdAsync store idx

        match mItem with
        | None ->
            return! RequestErrors.NOT_FOUND "No task found" next ctx
        | Some item ->
            ctx.SetStatusCode 200
            return! json item next ctx
    }

let handleAddTaskAsync =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let! createTask = ctx.BindJsonAsync<CreateTaskDTO>()
        let event = CreateTaskDTO.toEvent createTask (Security.Principal.WindowsIdentity.GetCurrent().Name)

        let publisher = ctx.GetService<Publisher>()
        publisher.DispatchAddSubscriptionEvent(event)

        return! Successful.NO_CONTENT next ctx
    }

let handleUpdateTaskAsync (idx: int) =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let! updateTask = ctx.BindJsonAsync<UpdateTaskStatusEvent>()
        let event:UpdateTaskStatusEvent = {
            Id = idx
            Type = updateTask.Type
            Status = updateTask.Status
            Updated_by = updateTask.Updated_by
        }

        let publisher = ctx.GetService<Publisher>()
        publisher.DispatchUpdateSubscriptionEvent(event)

        return! Successful.NO_CONTENT next ctx
    }

let handleDeleteTaskAsync (idx:int) =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let event:DeleteTaskEvent = {
                Id = idx
            }

        let publisher = ctx.GetService<Publisher>()
        publisher.DispatchDeleteTaskEvent(event)

        return! Successful.NO_CONTENT next ctx
    }
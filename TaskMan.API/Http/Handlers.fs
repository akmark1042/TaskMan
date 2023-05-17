module TaskMan.API.Http.Handlers

open Microsoft.AspNetCore.Http

open Giraffe

open TaskMan.Core.Interfaces
open TaskMan.Core.Default
open TaskMan.Core.Types
open TaskMan.API.Messaging.Publisher

let ofTask (item:Task) =
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

let handleGetAllTasks =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let store = ctx.GetService<ITaskStore>()
        let! result = GetAllTasksAsync store

        ctx.SetStatusCode 200
        return! json (result |> Seq.map ofTask |> Seq.toList) next ctx
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
        let! bindTask = ctx.BindJsonAsync<TaskDTO>()
        let switch = TaskDTO.toCreate bindTask
        let event:CreateTask = {
                Task_Name = switch.Task_Name
                Type = switch.Type
                Status = switch.Status
                Created_on = switch.Created_on
                Created_by = switch.Created_by
                Last_updated = switch.Last_updated
                Updated_by = switch.Updated_by
            }

        let publisher = ctx.GetService<Publisher>()
        publisher.DispatchAddSubscriptionEvent(event)

        return! Successful.NO_CONTENT next ctx
    }

let handleFinishTaskAsync idx =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let event:UpdateTaskStatusDTO = {
            Id = idx
        }

        let publisher = ctx.GetService<Publisher>()
        publisher.DispatchUpdateSubscriptionEvent(event)

        return! Successful.NO_CONTENT next ctx
    }

let handleDeleteTaskAsync (email:string) =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let event:DeleteTaskDTO = {
                Task_Name = email
            }

        let publisher = ctx.GetService<Publisher>()
        publisher.DispatchDeleteTaskEvent(event)

        return! Successful.NO_CONTENT next ctx
    }
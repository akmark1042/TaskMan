module TaskMan.API.Store

open System

open FSharp.Data.Sql

open TaskMan.Core.Database
open TaskMan.Core.Types
open TaskMan.Core.Interfaces

module Task =
    let ofRow (row:TaskDb.dataContext.``public.tasksEntity``) =
        {
            Id = row.Id
            Task_Name = row.TaskName
            Type = row.Type
            Status = row.Status
            Created_on = DateTimeOffset(row.CreatedOn)
            Created_by = row.CreatedBy
            Last_updated = DateTimeOffset(row.LastUpdated)
            Updated_by = row.UpdatedBy
        }

type TaskStore (logger: Serilog.ILogger, context:TaskDb.dataContext) =
    let logger = logger.ForContext<TaskStore>()

    interface ITaskStore with
        member this.getAllTasksAsync() : Async<TaskDTO list> =
            async {
                return query {
                    for row in context.Public.Tasks do
                    select row
                }
                |> Seq.map Task.ofRow
                |> Seq.toList
                |> List.map Task.toTaskDTO
            }
        
        member this.getTaskByIdAsync idx : Async<Option<TaskDTO>> =
            async {
                return query {
                    for row in context.Public.Tasks do
                    where (row.Id = idx)
                }
                |> Seq.tryExactlyOne
                |> Option.map Task.ofRow
                |> Option.map Task.toTaskDTO
            }
        
        member this.addTaskAsync (task_name: CreateTaskEvent) =
            async {
                let newRow = context.Public.Tasks.Create()
                newRow.TaskName <- task_name.Task_Name
                newRow.Type <- Some (task_name.Type.ToString())
                newRow.Status <- task_name.Status
                newRow.CreatedOn <- DateTime.Now
                newRow.CreatedBy <- task_name.Created_by
                newRow.LastUpdated <- DateTime.Now
                newRow.UpdatedBy <- task_name.Created_by
                context.SubmitUpdates()
            }
            
        member this.updateStatusAsync (idx:int) (upd:UpdateTaskStatusEvent) : Async<unit> =
            async {
                let mItem = query {
                    for row in context.Public.Tasks do
                        where (row.Id = idx)
                        select (Some row)
                        exactlyOneOrDefault
                }
                match mItem with
                | Some row ->
                    match row.Status with
                    | 3 ->
                        logger.Information(sprintf "This row %s cannot be changed while it is still in progress." row.TaskName)
                    | _ ->
                        row.Type <- Some upd.Type
                        row.Status <- upd.Status
                        row.LastUpdated <- DateTime.Now
                        row.UpdatedBy <- upd.Updated_by + idx.ToString()
                        row.OnConflict <- Common.OnConflict.Update
                        context.SubmitUpdates()
                | None -> ()
            }
        
        member this.deleteTaskAsync (task:int) : Async<int> =
            async {
                let mItem = query {
                    for row in context.Public.Tasks do
                        where (row.Id = task)
                        select (Some row)
                        exactlyOneOrDefault
                }
                match mItem with
                | None -> return 0
                | Some item ->
                    item.Delete()
                    context.SubmitUpdates()
                    return 1
            }
       
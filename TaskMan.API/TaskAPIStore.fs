module TaskMan.API.Store

open System

open FSharp.Data.Sql

open TaskMan.Core.Database
open TaskMan.Core.Types
open TaskMan.Core.Interfaces

let ofRow (row:TaskDb.dataContext.``public.tasksEntity``) : Task =
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

let ofDTORow (row:TaskDb.dataContext.``public.tasksEntity``) : TaskDTO =
    {
        Id = row.Id
        Task_Name = row.TaskName
        Type = (Option.defaultValue "" row.Type)
        Status = row.Status
        Created_on = DateTimeOffset(row.CreatedOn)
        Created_by = row.CreatedBy
        Last_updated = DateTimeOffset(row.LastUpdated)
        Updated_by = row.UpdatedBy
    }

type TaskStore (context:TaskDb.dataContext) =
    interface ITaskStore with
        member this.getAllTasksAsync() : Async<Task list> =
            async {
                let allList =
                    query {
                        for row in context.Public.Tasks do
                        select row
                    }
                    |> Seq.map ofRow
                    |> Seq.toList
                return allList
            }
        
        member this.getTaskByIdAsync idx : Async<Option<TaskDTO>> =
            async {
                let oneItem =
                    query {
                        for row in context.Public.Tasks do
                        where (row.Id = idx)
                    }
                    |> Seq.tryExactlyOne
                    |> Option.map ofDTORow
                    
                return oneItem
            }
        
        member this.addTaskAsync (taskName: CreateTask) : Async<TaskDTO> =
            async {
                let newRow = context.Public.Tasks.Create()
                newRow.TaskName <- taskName.Task_Name
                newRow.Type <- Some taskName.Type
                newRow.Status <- taskName.Status
                newRow.CreatedOn <- DateTime.Now
                newRow.CreatedBy <- taskName.Created_by
                newRow.LastUpdated <- DateTime.Now
                newRow.UpdatedBy <- taskName.Updated_by

                context.SubmitUpdates()

                let result =
                    query {
                        for row in context.Public.Tasks do
                        sortByDescending row.Id
                        head
                    }
                    |> ofDTORow
                
                return result
            }
            
        member this.finishTaskAsync idx : Async<Option<TaskDTO>> =
            async {
                let mItem = query {
                    for row in context.Public.Tasks do
                        where (row.Id = idx)
                        select (Some row)
                        exactlyOneOrDefault
                }
                match mItem with
                | Some row ->
                    row.Status <- Status.Complete |> int
                    row.LastUpdated <- DateTime.Now
                    row.UpdatedBy <- System.Security.Principal.WindowsIdentity.GetCurrent().Name + idx.ToString()
                    row.OnConflict <- Common.OnConflict.Update
                    context.SubmitUpdates()
                    return Some row |> Option.map ofDTORow
                | None -> return mItem |> Option.map ofDTORow
            }
        
        member this.deleteTaskAsync (task:string) : Async<int> =
            async {
                let mItem = query {
                    for row in context.Public.Tasks do
                        where (row.TaskName = task)
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
       
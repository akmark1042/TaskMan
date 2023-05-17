[<AutoOpen>]
module TaskMan.Core.Interfaces

open TaskMan.Core.Types

type ITaskStore =
    abstract getAllTasksAsync: unit -> Async<Task list>
    abstract addTaskAsync: CreateTask -> Async<TaskDTO>
    abstract getTaskByIdAsync: int -> Async<Option<TaskDTO>>
    abstract finishTaskAsync: int -> Async<Option<TaskDTO>>
    abstract deleteTaskAsync: string -> Async<int>

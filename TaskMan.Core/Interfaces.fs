[<AutoOpen>]
module TaskMan.Core.Interfaces

open TaskMan.Core.Types

type ITaskStore =
    abstract getAllTasksAsync: unit -> Async<TaskDTO list>
    abstract addTaskAsync: CreateTaskEvent -> Async<unit>
    abstract getTaskByIdAsync: int -> Async<Option<TaskDTO>>
    abstract updateStatusAsync: int -> UpdateTaskStatusEvent -> Async<unit>
    abstract deleteTaskAsync: int -> Async<int>

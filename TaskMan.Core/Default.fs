[<AutoOpen>]
module TaskMan.Core.Default

open TaskMan.Core.Interfaces

let GetAllTasksAsync (store: ITaskStore) =
     store.getAllTasksAsync()

let AddTaskAsync (store: ITaskStore) str =
     store.addTaskAsync str

let GetTaskByIdAsync (store: ITaskStore) idx =
     store.getTaskByIdAsync idx

let FinishTaskAsync (store: ITaskStore) idx =
     store.finishTaskAsync idx

let DeleteTaskAsync (store: ITaskStore) id =
     store.deleteTaskAsync id
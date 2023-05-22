[<AutoOpen>]
module TaskMan.Core.Default

open TaskMan.Core.Interfaces

let GetAllTasksAsync (store: ITaskStore) =
     store.getAllTasksAsync()

let AddTaskAsync (store: ITaskStore) str =
     store.addTaskAsync str

let GetTaskByIdAsync (store: ITaskStore) idx =
     store.getTaskByIdAsync idx

let UpdateStatusAsync (store: ITaskStore) idx =
     store.updateStatusAsync idx

let DeleteTaskAsync (store: ITaskStore) id =
     store.deleteTaskAsync id
module TaskMan.Client.Program

open System

open TaskMan.Client.Store
open TaskMan.Core.Types
open TaskMan.Client.Types

let rec loopAsync() =
    async {
        let operator = 3

        printfn "Pick a number between 1 and 6"
        let command = Console.ReadLine()

        match Int32.TryParse command with
        | false, _ -> printfn "Invalid entry"
        | true, i ->
            match i with
            | 1 ->
                printfn "Enter the name of the new task."
                let name = Console.ReadLine()
                let (createNew:NewTask) = {
                    Task_Name = name
                    Type = TaskType.Default.ToString()
                    Status = 0
                }

                let! newItem = addTaskAsync createNew
                printfn "%A" newItem
                return! loopAsync()
            | 2 ->
                //get one specified, unwrap the some
                let! theItem = getTaskAsync operator
                match theItem with
                | Some i -> printfn "%A" i
                | None -> printfn "No items found"

                return! loopAsync()
            | 3 ->
                let! allItems = getAllAsync()
                printfn "%A" allItems
                return! loopAsync()
            | 4 ->
                let (upd:UpdateTask) = {
                    Type = TaskType.Update.ToString()
                    Status = 3
                    Updated_by = System.Security.Principal.WindowsIdentity.GetCurrent().Name
                }
                let! result = updateStatusAsync operator upd
                return! loopAsync()
            | 5 ->
                let! returnInt = deleteTaskAsync operator
                printf "%i" returnInt
                return! loopAsync()
            | 6 ->
                printfn "\nBye!"
                exit(0)
            | _ -> return! loopAsync()
    }

[<EntryPoint>]
let main args = 
    async {
        do! loopAsync()
        return 0
    } |> Async.RunSynchronously
    
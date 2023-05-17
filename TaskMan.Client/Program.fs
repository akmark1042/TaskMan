module TaskMan.Client.Program

open TaskMan.Client.Store
open TaskMan.Core.Types
open System

let rec loopAsync() =
    async {
        let operator = 6

        printfn "Pick a number between 1 and 6"
        let command = Console.ReadLine()

        match Int32.TryParse command with
        | false, _ -> printfn "Invalid entry"
        | true, i ->
            match i with
            | 1 ->
                let createNew = {
                    Task_Name = "Nome"
                    Type = "testType"
                    Status = 0
                    Created_on = DateTimeOffset.Now
                    Created_by = System.Security.Principal.WindowsIdentity.GetCurrent().Name
                    Last_updated = DateTimeOffset.Now
                    Updated_by = System.Security.Principal.WindowsIdentity.GetCurrent().Name
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
                //get all
                let! allItems = getAllAsync()
                printfn "%A" allItems
                return! loopAsync()
            | 4 ->
                //change status to finished
                do! finishTaskAsync operator
                return! loopAsync()
            | 5 ->
                //remove
                let! returnInt = deleteTaskAsync "Girdwood"
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
    
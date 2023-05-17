module TaskMan.Client.Store

open System
open System.Net
open System.Net.Http
open System.Net.Http.Json

open TaskMan.Core.Types
open System.Net.Http.Headers

module Task =
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

let getClient() =
    let result = new HttpClient()
    result.DefaultRequestHeaders.Authorization <- new AuthenticationHeaderValue("Basic", "stub")
    result.BaseAddress <- new Uri("http://localhost:5000/api/")
    result

let getAllAsync() : Async<List<Task>> =
    async {
        use client = getClient()
        let! response = client.GetAsync("tasks") |> Async.AwaitTask
        
        if response.IsSuccessStatusCode |> not then
            return raise (sprintf "API returned unexpected status code: %O." response.StatusCode |> NotImplementedException)
        else
            let result = response.Content.ReadFromJsonAsync<List<Task>>() |> Async.AwaitTask
            return! result
    }

let getTaskAsync (idx:int)  =
    async {
        use client = getClient()
        let! response = sprintf "tasks/index/%i" idx |> client.GetAsync |> Async.AwaitTask
        
        if response.StatusCode = HttpStatusCode.NotFound then
            return None
        elif response.IsSuccessStatusCode |> not then
            return raise (sprintf "API returned unexpected status code: %O." response.StatusCode |> NotImplementedException)
        else
            let! result = response.Content.ReadFromJsonAsync<Task>() |> Async.AwaitTask
            return result |> Some
    }

let addTaskAsync (newTask: CreateTask) =
    async {
        let newItem = {
            Task_Name = newTask.Task_Name
            Type = newTask.Type
            Status = 0
            Created_on = DateTimeOffset.Now
            Created_by = System.Security.Principal.WindowsIdentity.GetCurrent().Name
            Last_updated = DateTimeOffset.Now
            Updated_by = newTask.Updated_by
        }

        use client = getClient()
        let! response = client.PostAsJsonAsync("tasks/create", newItem) |> Async.AwaitTask

        response.EnsureSuccessStatusCode() |> ignore
        
        return newItem
    }

let finishTaskAsync (idx:int) =
    async {
        use client = getClient()
        let! response = (sprintf "tasks/index/%i/finish" idx, null) |> client.PutAsync |> Async.AwaitTask

        response.EnsureSuccessStatusCode() |> ignore
    }

let deleteTaskAsync (id:string) =
    async {
        use client = getClient()
        let! response = sprintf "tasks/delete/%s" id |> client.DeleteAsync |> Async.AwaitTask
        
        if response.StatusCode = HttpStatusCode.NotFound then
            return 0
        else
            return 1
    }
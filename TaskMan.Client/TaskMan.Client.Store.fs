module TaskMan.Client.Store

open System
open System.Net
open System.Net.Http
open System.Net.Http.Json
open System.Text
open System.Text.Json

open TaskMan.Core.Types
open TaskMan.Client.Types
open System.Net.Http.Headers


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

let addTaskAsync (newTask: NewTask) =
    async {
        use client = getClient()
        let! response = client.PostAsJsonAsync("tasks/create", newTask) |> Async.AwaitTask
        response.EnsureSuccessStatusCode() |> ignore
    }

let updateStatusAsync (idx:int) (upd:UpdateTask) =
    async {
        use client = getClient()
        let httpContent = new StringContent(JsonSerializer.Serialize(upd), Encoding.UTF8, "application/json")
        let! response = (sprintf "tasks/index/%i/update" idx, httpContent) |> client.PutAsync |> Async.AwaitTask
        if response.StatusCode = HttpStatusCode.NotFound then
            return 0
        else
            return 1
    }

let deleteTaskAsync (id:int) =
    async {
        use client = getClient()
        let! response = sprintf "tasks/delete/%i" id |> client.DeleteAsync |> Async.AwaitTask
        
        if response.StatusCode = HttpStatusCode.NotFound then
            return 0
        else
            return 1
    }
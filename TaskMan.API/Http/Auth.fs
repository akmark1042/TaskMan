module TaskMan.API.Auth

open FSharp.Control.TaskBuilder
open Microsoft.AspNetCore.Http

open Giraffe

open TaskMan.API.Messaging.Types

let staticBasic token =
    fun (next:HttpFunc) (ctx:HttpContext) -> 
        task {
            match ctx.Request.Headers.Authorization |> Seq.toList |> AuthToken.make with
            | Ok authToken when (AuthToken.unwrap authToken) = token -> return! next ctx
            | _ -> return! (setStatusCode 401 >=> setHttpHeader "WWW-Authenticate" "Basic") earlyReturn ctx
        }

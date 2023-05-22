module TaskMan.API.Http.Routes

open Microsoft.AspNetCore.Http

open Giraffe

open TaskMan.API.Http.Handlers
open TaskMan.API.Auth

let webApp staticToken : HttpFunc -> HttpContext -> HttpFuncResult =
    choose [ subRoute
                "/api"
                (choose
                    [ staticBasic staticToken
                                 >=>
                                 subRoute "/tasks" //http://localhost:5000/api/tasks
                                    (choose
                                        [
                                             POST >=> routeCi "/create" >=> handleAddTaskAsync
                                             GET
                                             >=> choose [ route "" >=> handleGetAllTasks
                                                          routeCif "/index/%i" handleGetTaskById ]
                                             PUT >=> routeCif "/index/%i/update" handleUpdateTaskAsync
                                             DELETE >=> routef "/delete/%i" handleDeleteTaskAsync
                                        ]
                                    )
                    ]
                )
            ]
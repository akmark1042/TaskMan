module TaskMan.Client.Types

type NewTask =
    {
        Task_Name: string
        Type: string
        Status: int
    }

type UpdateTask =
    {
        Type: string
        Status: int
        Updated_by: string
    }
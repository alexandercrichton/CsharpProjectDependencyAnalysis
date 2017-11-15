[<EntryPoint>]
let main argv = 
    argv 
    |> List.ofArray
    |> App.main2
    |> printfn "%A"
    0
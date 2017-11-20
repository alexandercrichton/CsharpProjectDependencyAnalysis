open RopResult
open DomainTypes

[<EntryPoint>]
let main argv = 

    let result = 
        argv 
        |> List.ofArray
        |> App.run

    match result with
    | Success fileName ->
        printfn "Successfully created %A" fileName
    | Failure messages ->
        printfn "Errors: "
        for message in messages do
            match message with 
            | TooManyArguments _ -> 
                printfn "Too many arguments"
            | MissingDirectoryArgument _ ->
                printfn "Missing directory argument"
            | DirectoryDoesNotExist _ ->
                printfn "Directory does not exist"
            | NoSolutionsFound _ ->
                printfn "No solutions found in directory"
            | NoProjectsFoundInSolution solution ->
                printfn "No projects found in solution: %s" solution
            | InvalidProjectXml projectFile ->
                printfn "Invalid XML in project file: %s" projectFile
            | MultipleNugetFilesInProject project ->
                printfn "Multiple nuget files found in project: %s" project
            | InvalidNugetsXml nugetsFile ->
                printfn "Invalid XML in nuget packages file: %s" nugetsFile
        printfn "Usage: <exe> \"<solution-directory>\""

    0
open System.IO
open System

open DomainTypes

type Error =
    | TooManyArguments
    | MissingDirectoryArgument
    | DirectoryDoesNotExist
    | NoSolutionsFound
    | NoProjectsFoundInSolution of string

type Result<'T> =
    | Success of 'T
    | Failure of Error list

let onSuccess success result = 
    match result with
    | Success x -> success x
    | Failure errors -> Failure errors

let foldResults<'a> = 
    let fold state (result: Result<'a>) =
        match (state, result) with
        | (Success values), (Success value) -> Success (value::values)
        | Success _, Failure errors -> Failure errors
        | Failure errors, Success _ -> Failure errors
        | Failure a, Failure b -> Failure (b @ a)
    List.fold fold (Success [])
    
let directoryFromArguments = function
    | [directory] -> 
        if Directory.Exists directory then 
            Success (new DirectoryInfo(directory))
        else
            Failure [DirectoryDoesNotExist]
    | [] -> 
        Failure [MissingDirectoryArgument]
    | _ -> 
        Failure [TooManyArguments]

let loadProjects solution (solutionFile: FileInfo) =
    let projectFiles = solutionFile.Directory.GetFiles("*.csproj", SearchOption.AllDirectories)
    if projectFiles |> Array.isEmpty then
        Failure [NoProjectsFoundInSolution solution]
    else
        projectFiles
        |> List.ofArray
        |> List.map (fun (file: FileInfo) ->
            let projectName = file.Name.Split([|".csproj"|], StringSplitOptions.None) |> Array.head
            Success { name = projectName; dependencies = [] }
        )
        |> foldResults

let loadSolutions (directory: DirectoryInfo): Result<Solution list> =
    let solutionFiles = directory.GetFiles("*.sln", SearchOption.AllDirectories)
    if solutionFiles |> Array.isEmpty then
        Failure [NoSolutionsFound]
    else
        solutionFiles 
        |> List.ofArray 
        |> List.map (fun (file: FileInfo) ->
            let solutionName = file.Name.Split([|".sln"|], StringSplitOptions.None) |> Array.head
            let projects = loadProjects solutionName file
            match projects with
            | Success projects -> Success { name = solutionName; projects = projects }
            | Failure errors -> Failure errors
        )
        |> foldResults

let main2 args =
    args
    |> directoryFromArguments
    |> onSuccess loadSolutions


[<EntryPoint>]
let main argv = 
    argv 
    |> List.ofArray
    |> main2
    |> printfn "%A"
    0
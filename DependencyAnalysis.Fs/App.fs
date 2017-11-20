
module App

open RopResult
open DomainTypes
open System.IO

let private directoryFromArguments = function
    | [directory] -> 
        if Directory.Exists directory then 
            Success (new DirectoryInfo(directory))
        else
            Failure [DirectoryDoesNotExist]
    | [] -> 
        Failure [MissingDirectoryArgument]
    | _ -> 
        Failure [TooManyArguments]

type FileName = FileName of string
    
let private outputDgml (directory: DirectoryInfo) dgml =
    let fileName = sprintf "%s/output.dgml" directory.FullName
    File.WriteAllText(fileName, dgml)
    FileName fileName

let run args =
    args 
    |> directoryFromArguments
    |> RopResult.onSuccess (fun directory -> 
        directory
        |> Loading.loadSolutions
        |> RopResult.onSuccess (Dgml.build >> Success)
        |> RopResult.onSuccess (outputDgml directory >> Success)
    )
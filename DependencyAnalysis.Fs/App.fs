
module App

open Rop
open DomainTypes
open System.IO

let private directoryFromArguments = function
    | [directory] -> 
        if Directory.Exists directory then 
            Ok (new DirectoryInfo(directory))
        else
            Fail [DirectoryDoesNotExist]
    | [] -> 
        Fail [MissingDirectoryArgument]
    | _ -> 
        Fail [TooManyArguments]

type FileName = FileName of string
    
let private outputDgml (directory: DirectoryInfo) dgml =
    let fileName = sprintf "%s/output.dgml" directory.FullName
    File.WriteAllText(fileName, dgml)
    FileName fileName

let run args =
    args 
    |> directoryFromArguments
    |> Rop.bind (fun directory -> 
        directory
        |> Loading.loadSolutions
        |> Rop.bind (Dgml.build >> Ok)
        |> Rop.bind (outputDgml directory >> Ok)
    )
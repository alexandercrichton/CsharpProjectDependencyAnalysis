module Loading

open System.IO
open System.Xml.Linq
open System.Xml.XPath

open Rop
open DomainTypes

let tryParseXDocument xml =
    try 
        xml |> XDocument.Parse |> Some
    with    
    | _ -> None

let private loadProjectDependencies (projectFile: FileInfo) =
    projectFile.FullName 
    |> File.ReadAllText 
    |> tryParseXDocument
    |> Option.bind (fun xml ->
        xml.XPathSelectElements("//*[local-name() = 'ProjectReference']/*[local-name() = 'Name']")
        |> List.ofSeq
        |> List.map (fun element -> ProjectDependency element.Value)
        |> Ok
        |> Some
    )
    |> Option.defaultValue Rop.id

let foldListOfResults<'a, 'b> : (Result<'a, 'b> list -> Result<'a list, 'b>) =
    List.fold (fun a b -> Rop.bind2 (fun a b -> a::b) b a) Rop.id

let private loadNugetDependencies projectName (projectFile: FileInfo) =
    let nugetFiles = 
        projectFile.Directory
            .GetFiles("packages.config", SearchOption.TopDirectoryOnly)
            |> List.ofArray
    match nugetFiles with
    | [] -> Rop.id
    | [file] -> 
        file.FullName 
        |> File.ReadAllText 
        |> tryParseXDocument
        |> Option.bind (fun nugetsXml -> 
            nugetsXml.XPathSelectElements("//package")
            |> List.ofSeq
            |> List.map(fun element ->
                let attributeValue attributeName = 
                    element.Attributes() 
                    |> Seq.find (fun a -> a.Name.LocalName = attributeName) 
                    |> (fun a -> a.Value)
                let nugetName = attributeValue "id"
                let nugetVersion = attributeValue "version"
                NugetDependency { name = nugetName; version = nugetVersion }
            )
            |> Ok
            |> Some
        )
        |> Option.defaultValue Rop.id
    | _ -> Fail [MultipleNugetFilesInProject projectName]
    
let private loadProjects solution (solutionFile: FileInfo) =
    let projectFiles = solutionFile.Directory.GetFiles("*.csproj", SearchOption.AllDirectories)
    if projectFiles |> Array.isEmpty then
        Fail [NoProjectsFoundInSolution solution]
    else
        projectFiles
        |> List.ofArray
        |> List.map (fun (file: FileInfo) ->
            let projectName = file.Name |> Tools.trimFromEnd ".csproj"
            let projectDependencies = loadProjectDependencies file
            let nugetDependencies = loadNugetDependencies projectName file

            Rop.bind2 (fun a b -> a @ b) projectDependencies nugetDependencies
            |> Rop.map (fun dependencies -> 
                { name = projectName; dependencies = dependencies }
            )
        )
        |> foldListOfResults
        
let loadSolutions (directory: DirectoryInfo) =
    let solutionFiles = directory.GetFiles("*.sln", SearchOption.AllDirectories)
    if solutionFiles |> Array.isEmpty then
        Fail [NoSolutionsFound]
    else
        solutionFiles 
        |> List.ofArray 
        |> List.map (fun (file: FileInfo) ->
            let solutionName = file.Name |> Tools.trimFromEnd ".sln"
            loadProjects solutionName file
            |> Rop.map (fun projects -> { name = solutionName; projects = projects })
        )
        |> foldListOfResults
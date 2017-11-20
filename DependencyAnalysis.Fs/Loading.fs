module Loading

open System.IO
open System.Xml.Linq
open System.Xml.XPath

open RopResult
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
        |> Seq.map (fun element -> ProjectDependency element.Value)
        |> List.ofSeq
        |> Success
        |> Some
    )
    |> Option.defaultValue (Success [])

let private loadNugetDependencies projectName (projectFile: FileInfo) =
    let nugetFiles = 
        projectFile.Directory
            .GetFiles("packages.config", SearchOption.TopDirectoryOnly)
            |> List.ofArray
    match nugetFiles with
    | [] -> Success []
    | [file] -> 
        file.FullName 
        |> File.ReadAllText 
        |> tryParseXDocument
        |> Option.bind (fun nugetsXml -> 
            nugetsXml.XPathSelectElements("//package")
            |> Seq.map(fun element ->
                let attributeValue attributeName = 
                    element.Attributes() 
                    |> Seq.find (fun a -> a.Name.LocalName = attributeName) 
                    |> (fun a -> a.Value)
                let nugetName = attributeValue "id"
                let nugetVersion = attributeValue "version"
                Success (NugetDependency { name = nugetName; version = nugetVersion })
            )
            |> List.ofSeq
            |> foldResultList
            |> Some
        )
        |> Option.defaultValue (Success [])
    | _ -> Failure [MultipleNugetFilesInProject projectName]
    
let private loadProjects solution (solutionFile: FileInfo) =
    let projectFiles = solutionFile.Directory.GetFiles("*.csproj", SearchOption.AllDirectories)
    if projectFiles |> Array.isEmpty then
        Failure [NoProjectsFoundInSolution solution]
    else
        projectFiles
        |> List.ofArray
        |> List.map (fun (file: FileInfo) ->
            let projectName = file.Name |> Tools.trimFromEnd ".csproj"
            let dependencies = 
                loadProjectDependencies file
                |> mergeListResults (loadNugetDependencies projectName file)
            dependencies 
            |> RopResult.convert (fun dependencies -> 
                { name = projectName; dependencies = dependencies }
            )
        )
        |> foldResultList
        
let loadSolutions (directory: DirectoryInfo) =
    let solutionFiles = directory.GetFiles("*.sln", SearchOption.AllDirectories)
    if solutionFiles |> Array.isEmpty then
        Failure [NoSolutionsFound]
    else
        solutionFiles 
        |> List.ofArray 
        |> List.map (fun (file: FileInfo) ->
            let solutionName = file.Name |> Tools.trimFromEnd ".sln"
            loadProjects solutionName file
            |> RopResult.convert (fun projects -> { name = solutionName; projects = projects })
        )
        |> foldResultList
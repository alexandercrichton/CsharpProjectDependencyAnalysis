module DomainTypes

type Nuget = {
    name: string
    version: string
}

type Dependency =
    | ProjectDependency of string
    | NugetDependency of Nuget

type Project = {
    name: string
    dependencies: Dependency list
}

type Solution = {
    name: string
    projects: Project list
}

type Message =
    | TooManyArguments
    | MissingDirectoryArgument
    | DirectoryDoesNotExist
    | NoSolutionsFound
    | NoProjectsFoundInSolution of string
    | InvalidProjectXml of string
    | MultipleNugetFilesInProject of string
    | InvalidNugetsXml of string

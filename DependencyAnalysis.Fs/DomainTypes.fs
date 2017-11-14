module DomainTypes

type Nuget = {
    name: string
    version: string
}

type Dependency =
    | Project of string
    | Nuget of Nuget

type Project = {
    name: string
    dependencies: Dependency list
}

type Solution = {
    name: string
    projects: Project list
}
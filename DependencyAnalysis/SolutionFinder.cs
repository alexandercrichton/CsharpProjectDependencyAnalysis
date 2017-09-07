using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DependencyAnalysis
{
    internal class SolutionFinder
    {
        public static IReadOnlyCollection<Solution> FindSolutionsInDirectory(string directory)
        {
            var solutionFiles = new DirectoryInfo(directory).GetFiles("*.sln", SearchOption.AllDirectories);

            return solutionFiles.Select(BuildSolution).ToList();
        }

        private static Solution BuildSolution(FileInfo solutionFile)
        {
            var solutionName = solutionFile.Name.Split(new[] { ".sln" }, StringSplitOptions.None)[0];

            var projectFiles = solutionFile.Directory.GetFiles("*.csproj", SearchOption.AllDirectories)
                .Where(file => file.Length > 0);

            var projects = projectFiles.Select(BuildProject).ToList();

            return new Solution(solutionName, projects);
        }

        private static Project BuildProject(FileInfo projectFile)
        {
            var projectName = projectFile.Name.Split(new[] { ".csproj" }, StringSplitOptions.None)[0];

            var projectDependencies = ProjectDependencies(projectFile);
            var nugetDependencies = NugetDependencies(projectFile);

            return new Project(
                projectName,
                projectDependencies.Concat(nugetDependencies).ToList()
            );
        }

        private static IReadOnlyCollection<Dependency> ProjectDependencies(FileInfo projectFile)
        {
            var projectXml = XElement.Parse(File.ReadAllText(projectFile.FullName));

            var projectNameElements = projectXml
                .XPathSelectElements("//*[local-name() = 'ProjectReference']/*[local-name() = 'Name']")
                .ToList();

            return projectNameElements
                .Select(element => Dependency.ProjectDependency(element.Value))
                .ToList();
        }

        private static IReadOnlyCollection<Dependency> NugetDependencies(FileInfo projectFile)
        {
            var packagesFile = projectFile.Directory.GetFiles("packages.config", SearchOption.TopDirectoryOnly)
                .SingleOrDefault();
            if (packagesFile != null)
            {
                var xml = XElement.Parse(File.ReadAllText(packagesFile.FullName));
                var packageElements = xml.XPathSelectElements("//package").ToList();

                return packageElements
                    .Select(element =>
                    {
                        var name = element.Attributes().First(a => a.Name == "id").Value;
                        var version = element.Attributes().First(a => a.Name == "version").Value;
                        return Dependency.NugetDependency(name, version);
                    })
                    .ToList();
            }
            else
            {
                return Array.Empty<Dependency>();
            }
        }
    }
}

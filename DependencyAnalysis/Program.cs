using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DependencyAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            var (valid, directory) = DirectoryFromArguments(args);
            if (valid)
            {
                var projects = FindProjectsInDirectory(directory);

                OutputProjects(directory, projects);
            }
            else
            {
                ShowHelp();
            }

            Console.ReadKey();
        }

        private static (bool valid, string directory) DirectoryFromArguments(IReadOnlyCollection<string> args) =>
            args.Count == 1
                ? (true, args.First())
                : (false, null);

        private static IReadOnlyCollection<Project> FindProjectsInDirectory(string directory)
        {
            var projectFiles = new DirectoryInfo(directory).GetFiles("*.csproj", SearchOption.AllDirectories);

            var projectXmls = projectFiles
                .Select(file => (file, text: File.ReadAllText(file.FullName)))
                .Select(x => (x.file, xml: XElement.Parse(x.text)))
                .ToList();

            return projectXmls
                .Select(project =>
                {
                    var projectName = project.file.Name.Split(new[] { ".csproj" }, StringSplitOptions.None)[0];

                    var referenceElements = project.xml.XPathSelectElements("//*[local-name() = 'Reference']").ToList();
                    var includes = referenceElements
                        .Select(element =>
                            element.Attributes().Single(attribute =>
                                attribute.Name.LocalName == "Include"
                            )
                        )
                        .ToList();

                    var dependencies = includes
                        .Select(include =>
                        {
                            var split = include.Value.Split(',');

                            var dependencyName = split[0];

                            var versionString = split.SingleOrDefault(s => s.Contains("Version"));
                            var version = versionString?.Split('=').First();
                            return new ProjectId(dependencyName, version: version);
                        })
                        .ToList();

                    return new Project(new ProjectId(projectName), dependencies);
                })
                .ToList();
        }

        private static void OutputProjects(string directory, IReadOnlyCollection<Project> projects)
        {
            var filePath = $"{directory}/output.dgml";
            var output = Dgml(projects);
            File.WriteAllText(filePath, output);
        }

        private static string Dgml(IReadOnlyCollection<Project> projects) =>
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
<DirectedGraph Layout=""Sugiyama"" ZoomLevel=""-1"" xmlns=""http://schemas.microsoft.com/vs/2009/dgml"">
    {Nodes(projects)}
    {Links(projects)}
</DirectedGraph>";

        private static string Nodes(IReadOnlyCollection<Project> projects) =>
            $@"<Nodes>
                {projects
                    .Select(project => $@"<Node Id=""{project.Id}"" Label=""{project.Id}"" />")
                    .Aggregate("", (a, b) => a + b)
                }
            </Nodes>";

        private static string Links(IReadOnlyCollection<Project> projects) =>
            $@"<Links>
                {projects
                    .SelectMany(
                        project => project.Dependencies,
                        (project, dependency) => (source: project.Id, target: dependency)
                    )
                    .Select(x => $@"<Link Source=""{x.source}"" Target=""{x.target}"" />")
                    .Aggregate("", (a, b) => a + b)
                }
            </Links>";

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Usage: <AppName>.exe <directory>");
        }
    }
}

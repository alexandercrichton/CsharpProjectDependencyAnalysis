using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
                var solutions = FindSolutionsInDirectory(directory);

                OutputProjects(directory, solutions);
            }
            else
            {
                ShowHelp();
            }

            Console.WriteLine("Done");
            Task.Delay(1000).Wait();
        }

        private static (bool valid, string directory) DirectoryFromArguments(IReadOnlyCollection<string> args) =>
            args.Count == 1
                ? (true, args.First())
                : (false, null);

        private static IReadOnlyCollection<Solution> FindSolutionsInDirectory(string directory)
        {
            var solutionFiles = new DirectoryInfo(directory).GetFiles("*.sln", SearchOption.AllDirectories);

            return solutionFiles
                .Select(solutionFile =>
                {
                    var solutionName = solutionFile.Name.Split(new[] { ".sln" }, StringSplitOptions.None)[0];

                    var projectFiles = solutionFile.Directory.GetFiles("*.csproj", SearchOption.AllDirectories);

                    var projectXmls = projectFiles
                        .Select(file => (file, text: File.ReadAllText(file.FullName)))
                        .Select(x => (x.file, xml: XElement.Parse(x.text)))
                        .ToList();

                    var projects = projectXmls
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
                                    var version = versionString?.Split('=').Last();
                                    return new ProjectId(dependencyName, version: version);
                                })
                                .ToList();

                            return new Project(new ProjectId(projectName, solution: solutionName), dependencies);
                        })
                        .ToList();

                    return new Solution(solutionName, projects);
                })
                .ToList();
        }

        private static void OutputProjects(string directory, IReadOnlyCollection<Solution> solutions)
        {
            var filePath = $"{directory}/output.dgml";
            var output = Dgml(solutions);
            File.WriteAllText(filePath, output);
        }

        private static string Dgml(IReadOnlyCollection<Solution> solutions) =>
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
<DirectedGraph Layout=""Sugiyama"" ZoomLevel=""-1"" xmlns=""http://schemas.microsoft.com/vs/2009/dgml"">
    {Nodes(solutions)}
    {Links(solutions)}
</DirectedGraph>";

        private static string Nodes(IReadOnlyCollection<Solution> solutions) =>
            $@"<Nodes>
                {string.Join("", SolutionNodes(solutions))}
                {string.Join("", ProjectNodes(solutions))}
            </Nodes>";

        private static IEnumerable<string> SolutionNodes(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            select $@"<Node Id=""{solution.Name}"" Label=""{solution.Name}"" Group=""Expanded"" />";

        private static IEnumerable<string> ProjectNodes(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            select $@"<Node Id=""{project.Id}"" Label=""{project.Id}"" />";

        private static string Links(IReadOnlyCollection<Solution> solutions) =>
            $@"<Links>
                {string.Join("", SolutionToProjectLinks(solutions))}
                {string.Join("", ProjectToProjectLinks(solutions))}
            </Links>";

        private static IEnumerable<string> SolutionToProjectLinks(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            select $@"<Link Source=""{solution.Name}"" Target=""{project.Id}"" Category=""Contains"" />";

        private static IEnumerable<string> ProjectToProjectLinks(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            from dependency in project.Dependencies
            select $@"<Link Source=""{project.Id}"" Target=""{dependency}"" />";

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Usage: <AppName>.exe <directory>");
        }
    }
}

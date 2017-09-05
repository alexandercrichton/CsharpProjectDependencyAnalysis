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

                var test = (from solution in solutions
                 from project in solution.Projects
                 from dependency in project.Dependencies
                 where dependency.Name.Contains("System.Net.Http") && dependency.Version != null
                 select new { Solution = solution.Name, Project = project.Name, Dependency = dependency.Id })
                 .ToList();

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
                                    return new Dependency(dependencyName, version);
                                })
                                .ToList();

                            return new Project(
                                new ProjectId(projectName, solution: solutionName), 
                                projectName,
                                dependencies
                            );
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

        private const string DependencyVersionGroupCategoryId = "DependencyVersionGroup";

        private static string Dgml(IReadOnlyCollection<Solution> solutions) =>
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <DirectedGraph Layout=""Sugiyama"" ZoomLevel=""-1"" xmlns=""http://schemas.microsoft.com/vs/2009/dgml"">
                {Nodes(solutions)}
                {Links(solutions)}
               <Categories>  
                  <Category Id=""{DependencyVersionGroupCategoryId}"" Stroke=""Orange"" Background=""Green"" />  
               </Categories>  
            </DirectedGraph>";

        private static string Nodes(IReadOnlyCollection<Solution> solutions) =>
            $@"<Nodes>
                {string.Join("", SolutionNodes(solutions))}
                {string.Join("", ProjectNodes(solutions))}
                {string.Join("", SingleVersionDependencyNodes(solutions))}
                {string.Join("", GroupedVersionDependencyNodes(solutions))}
            </Nodes>";

        private static IEnumerable<string> SolutionNodes(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            select $@"<Node Id=""{solution.Name}"" Label=""{solution.Name}"" Group=""Expanded"" />";

        private static IEnumerable<string> ProjectNodes(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            group project by project.Name into grouping
            where grouping.Count() == 1
            select $@"<Node Id=""{grouping.Single().Id}"" Label=""{grouping.Single().Id}"" />";

        private static IEnumerable<string> SingleVersionDependencyNodes(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            from dependency in project.Dependencies
            group dependency by dependency.Name into grouping
            where grouping.Select(dependency => dependency.Version).Distinct().Count() == 1
            select $@"<Node Id=""{grouping.First().Id}"" Label=""{grouping.First().Id}"" />";

        private static IEnumerable<string> GroupedVersionDependencyNodes(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            from dependency in project.Dependencies
            group dependency by dependency.Name into grouping
            where grouping.Select(dependency => dependency.Version).Distinct().Count() > 1
            select string.Join(
                "",
                grouping
                    .Select(dependency => $@"<Node Id=""{dependency.Id}"" Label=""{dependency.Id}"" />")
                    .Concat(new[] 
                    {
                        $@"<Node Id=""{grouping.Key}"" Label=""{grouping.Key}"" Group=""Expanded"" Category=""{DependencyVersionGroupCategoryId}"" />"
                    })
            );

        private static string Links(IReadOnlyCollection<Solution> solutions) =>
            $@"<Links>
                {string.Join("", SolutionToProjectLinks(solutions))}
                {string.Join("", ProjectToDependencyLinks(solutions))}
                {string.Join("", DependencyGroupToDependencyLinks(solutions))}
            </Links>";

        private static IEnumerable<string> SolutionToProjectLinks(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            select $@"<Link Source=""{solution.Name}"" Target=""{project.Id}"" Category=""Contains"" />";

        private static IEnumerable<string> ProjectToDependencyLinks(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            from dependency in project.Dependencies
            select $@"<Link Source=""{project.Id}"" Target=""{dependency.Id}"" />";

        private static IEnumerable<string> DependencyGroupToDependencyLinks(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            from dependency in project.Dependencies
            group dependency by dependency.Name into grouping
            where grouping.Select(dependency => dependency.Version).Distinct().Count() > 1
            select string.Join(
                "",
                grouping.Select(dependency => 
                    $@"<Link Source=""{grouping.Key}"" Target=""{dependency.Id}"" Category=""Contains"" />"
                )
            );

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Usage: <AppName>.exe <directory>");
        }
    }
}

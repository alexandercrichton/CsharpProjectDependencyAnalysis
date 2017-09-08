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
                var solutions = SolutionFinder.FindSolutionsInDirectory(directory);

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

        private static void OutputProjects(string directory, IReadOnlyCollection<Solution> solutions)
        {
            var filePath = $"{directory}/output.dgml";
            var output = Dgml(solutions);
            File.WriteAllText(filePath, output);
        }

        private const string ProjectMultipleVersionsGroupCategoryId = nameof(ProjectMultipleVersionsGroupCategoryId);
        private const string SolutionCategoryId = nameof(SolutionCategoryId);

        private static string Dgml(IReadOnlyCollection<Solution> solutions) =>
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <DirectedGraph Layout=""Sugiyama"" ZoomLevel=""-1"" xmlns=""http://schemas.microsoft.com/vs/2009/dgml"">
                {Nodes(solutions)}
                {Links(solutions)}
               <Categories>  
                  <Category Id=""{ProjectMultipleVersionsGroupCategoryId}"" Background=""Orange"" />  
                  <Category Id=""{SolutionCategoryId}"" Background=""#2c89cc"" />  
               </Categories>  
            </DirectedGraph>";

        private static string Nodes(IReadOnlyCollection<Solution> solutions) =>
            $@"<Nodes>
                {string.Join("", SolutionNodes(solutions))}
                {string.Join("", ProjectNodes(solutions))}
            </Nodes>";

        private static IEnumerable<string> SolutionNodes(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            select $@"<Node Id=""{SolutionId(solution.Name)}"" Label=""{solution.Name}"" Group=""Expanded"" Category=""{SolutionCategoryId}"" />";

        private static string SolutionId(string name) =>
            $"Sln-{name}";

        private static IEnumerable<string> ProjectNodes(IReadOnlyCollection<Solution> solutions) =>
            ProjectVersionGroups(solutions).SelectMany(projectGroup =>
            {
                if (projectGroup.Versions.Any())
                {
                    var category = projectGroup.Versions.Count > 1
                        ? ProjectMultipleVersionsGroupCategoryId
                        : "";
                    return new[] { $@"<Node Id=""{projectGroup.ProjectName}"" Group=""Expanded"" Category=""{category}"" />" }
                        .Concat(projectGroup.Versions.Select(version =>
                            $@"<Node Id=""{ProjectId(projectGroup.ProjectName, version)}"" />"
                        ));
                }
                else
                {
                    return new[] { $@"<Node Id=""{ProjectId(projectGroup.ProjectName)}"" />" };
                }
            });

        private static string Links(IReadOnlyCollection<Solution> solutions) =>
            $@"<Links>
                {string.Join("", SolutionToProjectLinks(solutions))}
                {string.Join("", ProjectToVersionLinks(solutions))}
                {string.Join("", ProjectToDependencyLinks(solutions))}
            </Links>";

        private static IEnumerable<string> SolutionToProjectLinks(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            select $@"<Link Source=""{SolutionId(solution.Name)}"" Target=""{ProjectId(project.Name)}"" Category=""Contains"" />";

        private static IEnumerable<string> ProjectToVersionLinks(IReadOnlyCollection<Solution> solutions) =>
            from projectGroup in ProjectVersionGroups(solutions)
            where projectGroup.Versions.Any()
            from version in projectGroup.Versions
            let sourceId = ProjectId(projectGroup.ProjectName)
            let targetId = ProjectId(projectGroup.ProjectName, version)
            select $@"<Link Source=""{sourceId}"" Target=""{targetId}"" Category=""Contains"" />";

        private static IEnumerable<string> ProjectToDependencyLinks(IReadOnlyCollection<Solution> solutions) =>
            from solution in solutions
            from project in solution.Projects
            from dependency in project.Dependencies
            select $@"<Link Source=""{ProjectId(project.Name)}"" Target=""{ProjectId(dependency.Name, dependency.Version)}"" />";
        
        private static IEnumerable<ProjectVersionGroup> ProjectVersionGroups(IReadOnlyCollection<Solution> solutions)
        {
            var projects = solutions.SelectMany(solution => solution.Projects).ToList();
            var dependencies = projects.SelectMany(project => project.Dependencies).ToList();

            var projectVersions = projects
                .Select(project => new { project.Name, Version = (string)null })
                .Concat(dependencies.Select(dependency => new { dependency.Name, dependency.Version }))
                .Distinct()
                .ToList();

            var groups = projectVersions.GroupBy(p => p.Name);

            return groups.Select(group =>
                new ProjectVersionGroup(
                    group.Key, 
                    group
                        .Select(p => p.Version)
                        .Where(version => version != null)
                        .ToList()
                )
            );
        }

        private static string ProjectId(string name, string version = null) =>
            $"{name}{(version != null ? $"[{version}]" : "")}";

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Usage: <AppName>.exe <directory>");
        }
    }
}

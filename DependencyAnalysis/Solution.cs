using System.Collections.Generic;

namespace DependencyAnalysis
{
    internal class Solution
    {
        public Solution(string name, IReadOnlyCollection<Project> projects)
        {
            Name = name;
            Projects = projects;
        }

        public string Name { get; }

        public IReadOnlyCollection<Project> Projects { get; }

        public override string ToString() =>
            Name;
    }
}

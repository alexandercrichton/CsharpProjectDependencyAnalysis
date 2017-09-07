using System.Collections.Generic;

namespace DependencyAnalysis
{
    internal class ProjectVersionGroup
    {
        public ProjectVersionGroup(string projectName, IReadOnlyCollection<string> versions)
        {
            ProjectName = projectName;
            Versions = versions;
        }

        public string ProjectName { get; }

        public IReadOnlyCollection<string> Versions { get; }

        public override string ToString() =>
            $"{{ {nameof(ProjectName)}: {ProjectName}, {nameof(Versions)}: [ {string.Join(", ", Versions)} ] }}";
    }
}

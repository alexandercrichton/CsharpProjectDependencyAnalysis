using System.Collections.Generic;

namespace DependencyAnalysis
{
    internal class Project
    {
        public Project(ProjectId id, IReadOnlyCollection<ProjectId> dependencies)
        {
            Id = id;
            Dependencies = dependencies;
        }

        public ProjectId Id { get; }

        public IReadOnlyCollection<ProjectId> Dependencies { get; }

        public override string ToString() =>
            $"{{ {nameof(Id)}: {Id}, {nameof(Dependencies)}: [ {string.Join(", ", Dependencies)} ] }}";
    }
}

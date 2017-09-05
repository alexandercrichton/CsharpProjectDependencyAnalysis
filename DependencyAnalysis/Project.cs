using System.Collections.Generic;

namespace DependencyAnalysis
{
    internal class Project
    {
        public Project(ProjectId id, string name, IReadOnlyCollection<Dependency> dependencies)
        {
            Id = id;
            Name = name;
            Dependencies = dependencies;
        }

        public string Name { get; }

        public ProjectId Id { get; }

        public IReadOnlyCollection<Dependency> Dependencies { get; }

        public override string ToString() =>
            $"{{ {nameof(Id)}: {Id}, {nameof(Dependencies)}: [ {string.Join(", ", Dependencies)} ] }}";
    }
}

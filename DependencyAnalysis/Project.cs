using System.Collections.Generic;

namespace DependencyAnalysis
{
    internal class Project
    {
        public Project(string name, IReadOnlyCollection<Dependency> dependencies)
        {
            Name = name;
            Dependencies = dependencies;
        }

        public string Name { get; }

        public IReadOnlyCollection<Dependency> Dependencies { get; }

        public override string ToString() =>
            $"{{ {nameof(Name)}: {Name}, {nameof(Dependencies)}: [ {string.Join(", ", Dependencies)} ] }}";
    }
}

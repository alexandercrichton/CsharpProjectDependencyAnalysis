namespace DependencyAnalysis
{
    internal class Dependency
    {
        private Dependency(string name, string version = null)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }

        public string Version { get; }

        public static Dependency ProjectDependency(string name) =>
            new Dependency(name, null);

        public static Dependency NugetDependency(string name, string version) =>
            new Dependency(name, version);

        public override string ToString() =>
            $"{{ {nameof(Name)}: {Name}, {nameof(Version)}: {(Version is null ? "null" : Version)} }}";
    }
}

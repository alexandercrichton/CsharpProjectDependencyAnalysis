namespace DependencyAnalysis
{
    internal class Dependency
    {
        public Dependency(string name, string version)
        {
            Id = new ProjectId(name, version: version);
            Name = name;
            Version = version;
        }

        public ProjectId Id { get; }

        public string Name { get; }

        public string Version { get; }
    }
}

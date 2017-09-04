namespace DependencyAnalysis
{
    internal class Dependency
    {
        public Dependency(string name, string version)
        {
            ProjectId = new ProjectId(name, version: version);
            Name = name;
            Version = version;
        }

        public ProjectId ProjectId { get; }

        public string Name { get; }

        public string Version { get; }
    }
}

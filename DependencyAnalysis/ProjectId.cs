using System;
using System.Collections.Generic;

namespace DependencyAnalysis
{
    internal class ProjectId : IEquatable<ProjectId>
    {
        public ProjectId(string name, string version = null, string solution = null)
        {
            Name = name;
            Version = version;
            Solution = solution;
        }

        public string Name { get; }

        public string Version { get; }

        public string Solution { get; }

        public override bool Equals(object other) =>
            Equals(other as ProjectId);

        public bool Equals(ProjectId other) =>
            other?.Name == Name && other?.Version == Version;

        public static bool operator ==(ProjectId a, ProjectId b) =>
            Equals(a, b);

        public static bool operator !=(ProjectId a, ProjectId b) =>
            !(a == b);

        public override string ToString() =>
            $"{Solution}{Name}{(Version != null ? $"[{Version}]" : "")}";

        public override int GetHashCode()
        {
            var hashCode = 2112831277;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Version);
            return hashCode;
        }
    }
}

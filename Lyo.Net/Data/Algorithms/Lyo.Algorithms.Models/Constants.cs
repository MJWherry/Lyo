namespace Lyo.Algorithms.Models;

/// <summary>Shared constants for the Algorithms HTTP surface.</summary>
public static class Constants
{
    /// <summary>REST API route names.</summary>
    public static class Rest
    {
        /// <summary>Algorithm catalog routes used by <c>BuildAlgorithmsGroup</c> and <c>IAlgorithmClient</c>.</summary>
        public static class Algorithms
        {
            /// <summary>OpenAPI tag and route root. <c>GET</c> lists definitions.</summary>
            public const string Route = "Algorithms";

            /// <summary>One definition by key.</summary>
            public static string Definition(string key) => $"{Route}/{key}";

            /// <summary>Revision list and append.</summary>
            public static string Revisions(string key) => $"{Route}/{key}/revisions";

            /// <summary>Copy a snapshot onto a new current revision.</summary>
            public static string Revert(string key) => $"{Route}/{key}/revert";

            /// <summary>One label on a definition.</summary>
            public static string Label(string key, string label) => $"{Route}/{key}/labels/{label}";
        }
    }
}

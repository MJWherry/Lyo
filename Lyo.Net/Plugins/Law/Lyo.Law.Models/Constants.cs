namespace Lyo.Law.Models;

/// <summary>Shared constants for the Law HTTP surface.</summary>
public static class Constants
{
    /// <summary>REST API route names.</summary>
    public static class Rest
    {
        /// <summary>Law route prefixes used by <c>BuildLawGroup</c> and Blazor clients.</summary>
        public static class Law
        {
            /// <summary>OpenAPI tag and route root.</summary>
            public const string Route = "Law";

            /// <summary>State jurisdiction CRUD.</summary>
            public const string Jurisdictions = $"{Route}/Jurisdictions";

            /// <summary>Named statutory compilations inside a state.</summary>
            public const string Codes = $"{Route}/Codes";

            /// <summary>Statute nodes. Query and get only; body changes go through <see cref="NodesBySource"/>.</summary>
            public const string Nodes = $"{Route}/Nodes";

            /// <summary>Official notes. Query and get only; upsert replaces the set.</summary>
            public const string Annotations = $"{Route}/Annotations";

            /// <summary>Prior node text. Query and get only.</summary>
            public const string Revisions = $"{Route}/Revisions";

            /// <summary>Create or update one node by jurisdiction, code, and source key.</summary>
            public const string NodesBySource = $"{Nodes}/by-source";

            /// <summary>One node, including body and annotations.</summary>
            public static string Node(Guid id) => $"{Nodes}/{id:D}";

            /// <summary>Direct children of a node, sort order then label.</summary>
            public static string NodeChildren(Guid id) => $"{Nodes}/{id:D}/children";

            /// <summary>Parent chain, nearest parent first.</summary>
            public static string NodeAncestors(Guid id) => $"{Nodes}/{id:D}/ancestors";

            /// <summary>One node addressed by jurisdiction, code, and citation.</summary>
            public static string NodeByCitation(string jurisdictionKey, string codeKey, string citation)
                => $"{Nodes}/by-citation?jurisdictionKey={Uri.EscapeDataString(jurisdictionKey)}&codeKey={Uri.EscapeDataString(codeKey)}&citation={Uri.EscapeDataString(citation)}";
        }
    }
}

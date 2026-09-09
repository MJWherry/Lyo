namespace Lyo.Authentication.Models;

/// <summary>Shared constants for authentication management HTTP routes and related names.</summary>
public static class Constants
{
    /// <summary>REST API route names for the auth admin surfaces mapped by <c>Lyo.Api.Authentication</c>.</summary>
    public static class Rest
    {
        /// <summary>Auth admin route group.</summary>
        public static class Auth
        {
            /// <summary>Map group prefix without a leading slash.</summary>
            public const string Route = "Auth";

            /// <summary>Lyo user QueryProject / Get / Patch.</summary>
            public const string User = $"{Route}/User";

            /// <summary>Opaque token QueryProject / Get / Patch / Delete.</summary>
            public const string Token = $"{Route}/Token";

            /// <summary>User-claim CRUD.</summary>
            public const string Claim = $"{Route}/Claim";

            /// <summary>User-scope CRUD.</summary>
            public const string Scope = $"{Route}/Scope";

            /// <summary>Linked identity QueryProject / Get (read-only).</summary>
            public const string LinkedIdentity = $"{Route}/LinkedIdentity";

            /// <summary>Auth audit event QueryProject / Get (read-only).</summary>
            public const string Event = $"{Route}/Event";
        }
    }
}

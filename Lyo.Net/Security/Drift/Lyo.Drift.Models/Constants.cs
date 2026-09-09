namespace Lyo.Drift.Models;

/// <summary>Shared constants for the Drift collector.</summary>
public static class Constants
{
    /// <summary>REST API route names.</summary>
    public static class Rest
    {
        /// <summary>Collector routes under <c>Drift/</c>.</summary>
        public static class Drift
        {
            public const string Route = "Drift";
            public const string Instances = $"{Route}/Instance";
            public const string InstanceUpsert = $"{Instances}/Upsert";
            public const string Snapshots = $"{Route}/Snapshot";
            public const string Diffs = $"{Route}/Diff";
            public const string Changes = $"{Route}/Change";

            /// <summary>PATCH heartbeat for one instance.</summary>
            public static string InstanceHeartbeat(Guid instanceId) => $"{Instances}/{instanceId}/Heartbeat";

            /// <summary>POST stop for one instance.</summary>
            public static string InstanceStop(Guid instanceId) => $"{Instances}/{instanceId}/Stop";

            /// <summary>POST server-side diff of two stored snapshots.</summary>
            public static string DiffAgainst(Guid snapshotId, Guid otherId) => $"{Snapshots}/{snapshotId}/DiffAgainst/{otherId}";
        }
    }
}

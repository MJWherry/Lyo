namespace Lyo.Story.Models;

/// <summary>Shared constants for the Story HTTP surface.</summary>
public static class Constants
{
    /// <summary>REST API route names.</summary>
    public static class Rest
    {
        /// <summary>Story routes used by <c>BuildStoryGroup</c> and <c>IStoryClient</c>.</summary>
        public static class Story
        {
            /// <summary>OpenAPI tag.</summary>
            public const string Route = "Story";

            /// <summary>Story row CRUD. List and filter use Query on this route.</summary>
            public const string Stories = "Story/Stories";

            /// <summary>Scene row CRUD.</summary>
            public const string Scenes = "Story/Scenes";

            /// <summary>Node row CRUD.</summary>
            public const string Nodes = "Story/Nodes";

            /// <summary>Variable row CRUD.</summary>
            public const string Variables = "Story/Variables";

            /// <summary>Action row CRUD.</summary>
            public const string Actions = "Story/Actions";

            /// <summary>Choice row CRUD.</summary>
            public const string Choices = "Story/Choices";

            /// <summary>Reaction row CRUD.</summary>
            public const string Reactions = "Story/Reactions";

            /// <summary>Effect row CRUD.</summary>
            public const string Effects = "Story/Effects";

            /// <summary>Read-only playthrough rows.</summary>
            public const string Playthroughs = "Story/Playthroughs";

            /// <summary>Aggregate graph save.</summary>
            public const string Graph = "Story/graph";

            /// <summary>List story headers for a tenant and game.</summary>
            public const string GraphList = "Story/graph/list";

            /// <summary>Start a playthrough.</summary>
            public const string PlayStart = "Story/playthrough/start";

            /// <summary>Aggregate graph by id.</summary>
            public static string GraphById(Guid id) => $"{Graph}/{id:D}";

            /// <summary>Aggregate graph by slug.</summary>
            public static string GraphBySlug(string slug) => $"{Graph}/by-slug/{slug}";

            /// <summary>Playthrough snapshot.</summary>
            public static string Play(Guid id) => $"Story/playthrough/{id:D}";

            /// <summary>Moves available on a playthrough.</summary>
            public static string Moves(Guid id) => $"Story/playthrough/{id:D}/moves";

            /// <summary>Take a choice.</summary>
            public static string Choose(Guid id) => $"Story/playthrough/{id:D}/choose";

            /// <summary>Advance a cutscene or silent beat.</summary>
            public static string Advance(Guid id) => $"Story/playthrough/{id:D}/advance";

            /// <summary>Write externally owned variable values.</summary>
            public static string SetVariables(Guid id) => $"Story/playthrough/{id:D}/variables";

            /// <summary>Play log.</summary>
            public static string Events(Guid id) => $"Story/playthrough/{id:D}/events";

            /// <summary>Events that caused a variable's current value.</summary>
            public static string Causes(Guid id, string variableKey) => $"Story/playthrough/{id:D}/causes/{variableKey}";
        }
    }
}
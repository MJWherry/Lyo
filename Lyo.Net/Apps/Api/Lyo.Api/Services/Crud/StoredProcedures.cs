namespace Lyo.Api.Services.Crud;

/// <summary>Well-known PostgreSQL function names used by jobs and reporting, fully qualified as <c>schema.name</c>.</summary>
public static class StoredProcedures
{
    /// <summary>General-purpose analytics helper names.</summary>
    public static class Info
    {
        public const string UniqueValuesWithCount = "public.sp_get_unique_values_with_counts";
    }

    /// <summary>Job subsystem stored-procedure names.</summary>
    public static class Job
    {
        public const string Statistics = "public.sp_GetJobStats";
    }
}
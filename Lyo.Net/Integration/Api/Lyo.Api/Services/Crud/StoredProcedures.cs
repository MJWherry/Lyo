namespace Lyo.Api.Services.Crud;

public static class StoredProcedures
{
    public static class Info
    {
        public const string UniqueValuesWithCount = "public.sp_get_unique_values_with_counts";
    }

    public static class Job
    {
        public const string Statistics = "public.sp_GetJobStats";
    }
}
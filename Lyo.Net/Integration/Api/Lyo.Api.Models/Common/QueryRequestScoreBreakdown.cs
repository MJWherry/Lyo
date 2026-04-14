namespace Lyo.Api.Models.Common;

public sealed record QueryRequestScoreBreakdown(
    int TotalScore,
    int PagingScore,
    int KeysScore,
    int SortScore,
    int IncludeScore,
    int ComputedFieldsScore,
    int TotalCountModeScore,
    int WhereClauseScore,
    int Start,
    int Amount,
    int IncludeCount,
    int IncludeMaxDepth,
    int IncludeTotalPathSegments,
    int SortCount,
    int KeyCount,
    int WhereClauseCount,
    int QueryConditionCount,
    int QueryGroupClauseCount,
    int WhereClauseMaxDepth,
    int QuerySubClauseCount,
    int QuerySubClauseMaxDepth,
    int QueryMaxGroupBranchingFactor)
{
    public static QueryRequestScoreBreakdown Empty() => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

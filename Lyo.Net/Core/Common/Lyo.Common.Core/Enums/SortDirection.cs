using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

public enum SortDirection
{
    /// <summary>Sort from low to high</summary>
    [Description("Description")]
    Asc = 0,

    /// <summary>Sort from high to low</summary>
    [Description("Descending")]
    Desc = 1
}
namespace Lyo.Api.Models.Enums;

/// <summary>How a JSON body is walked. The default is a root array, which is what JSON export writes.</summary>
public enum ImportJsonMode
{
    /// <summary>Root JSON array. A root object is a document error.</summary>
    Array,

    /// <summary>Root object. <c>RowsPath</c> names the array property.</summary>
    Wrapped,

    /// <summary>One JSON value per line.</summary>
    Ndjson
}
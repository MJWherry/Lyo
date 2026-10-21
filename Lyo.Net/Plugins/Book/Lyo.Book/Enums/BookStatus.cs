namespace Lyo.Book.Enums;

/// <summary>Publication status of a catalog book (one edition / ISBN row).</summary>
public enum BookStatus
{
    Unknown = 0,
    Announced = 1,
    Published = 2,
    OutOfPrint = 3,
    Unpublished = 4
}
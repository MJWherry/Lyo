namespace Lyo.Api.Import;

/// <summary>Host save hook. <see cref="IImportService" /> does not call it. The host saves the rows it received.</summary>
public interface IImportCommitter
{
    /// <summary>Persists rows. Import does not invoke this.</summary>
    void Save<T>(IReadOnlyList<T> rows);
}

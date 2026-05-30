namespace Lyo.EntityReference.Models;

/// <summary>Helpers for <see cref="IEntitySourceDerived" />.</summary>
public static class EntitySourceDerivedExtensions
{
    extension(IEntitySourceDerived entity)
    {
        /// <summary>Whether this row was imported from an external source.</summary>
        public bool HasSource() => entity.Source is not null;

        /// <summary>Whether this row has local edits since import.</summary>
        public bool IsLocallyModified() => entity.LocallyModifiedAt != null;
    }
}

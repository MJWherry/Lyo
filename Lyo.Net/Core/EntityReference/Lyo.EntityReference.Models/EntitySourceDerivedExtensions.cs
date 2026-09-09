namespace Lyo.EntityReference.Models;

/// <summary>Helpers for <see cref="IEntitySourceDerived" />.</summary>
public static class EntitySourceDerivedExtensions
{
    extension(IEntitySourceDerived entity)
    {
        /// <summary>True when this row was imported from an external source.</summary>
        public bool HasSource() => entity.Source is not null;

        /// <summary>True when this row has local edits since import.</summary>
        public bool IsLocallyModified() => entity.LocallyModifiedAt != null;
    }
}
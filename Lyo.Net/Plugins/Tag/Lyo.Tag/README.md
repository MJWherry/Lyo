# Lyo.Tag

Contracts for hanging tags on entities. The target is an `EntityRef`, and the person or system that applied the tag is an optional second `EntityRef`. A feature can attach tags without tying them through a foreign key.

## `ITagStore`

- `AddTagAsync(EntityRef forEntity, string tag, string tagType = "tag", EntityRef? fromEntity = null, string? slug = null, CancellationToken ct = default)` writes a tag. The same `(forEntity, tag, tagType, slug)` tuple is a no-op if it already exists. A null or whitespace `slug` becomes empty. Leave `fromEntity` off and the well-known system actor is recorded.
- `RemoveTagAsync(EntityRef forEntity, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default)` drops the matching assignment. The `slug` you pass has to match what was stored, including empty when nothing was stored.
- `GetTagsForEntityAsync(EntityRef forEntity, string? tagType = null, CancellationToken ct = default)` lists every `TagRecord` on that entity. Pass `tagType` to narrow the result.
- `GetEntitiesWithTagAsync(string tag, string? forEntityType = null, string? tagType = null, CancellationToken ct = default)` lists every entity that carries that tag value. Entity type and tag type filters are optional.
- `GetAllTagsForEntityTypeAsync(string forEntityType, string? tagType = null, CancellationToken ct = default)` lists the distinct tag values used on any entity of that type.
- `RemoveAllTagsForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` soft-deletes every tag assignment on that entity.

## `TagRecord`

- `Name`. Display text for the tag, such as `"urgent"`.
- `TagType`. Namespace discriminator. Default is `"tag"`. Other usual values include `"category"` and `"label"`.
- `Slug`. Optional slug suitable for URLs. Empty if none was given.
- `SubjectRef` / `ActorRef`. Projected as `EntityRef`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
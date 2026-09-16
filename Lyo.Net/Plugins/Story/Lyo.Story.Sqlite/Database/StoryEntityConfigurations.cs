using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Story.Sqlite.Database;

public sealed class StoryEntityConfiguration : IEntityTypeConfiguration<StoryEntity>
{
    public void Configure(EntityTypeBuilder<StoryEntity> builder)
    {
        builder.ToTable("story");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.GameKey).HasMaxLength(100).HasColumnName("game_key");
        builder.Property(e => e.Description).HasMaxLength(8192).HasColumnName("description");
        builder.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.Status).HasConversion<string>().HasColumnName("status");
        builder.Property(e => e.StartNodeId).HasColumnName("start_node_id");
        builder.Property(e => e.CoverImageRef).HasMaxLength(512).HasColumnName("cover_image_ref");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_story_story_slug");
        builder.HasIndex(e => new { e.TenantId, e.GameKey }).HasDatabaseName("ix_story_story_tenant_game");
        builder.HasOne(e => e.StartNode).WithMany().HasForeignKey(e => e.StartNodeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.Scenes).WithOne(s => s.Story).HasForeignKey(s => s.StoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Nodes).WithOne(n => n.Story).HasForeignKey(n => n.StoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Variables).WithOne(v => v.Story).HasForeignKey(v => v.StoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Actions).WithOne(a => a.Story).HasForeignKey(a => a.StoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Reactions).WithOne(r => r.Story).HasForeignKey(r => r.StoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Playthroughs).WithOne(p => p.Story).HasForeignKey(p => p.StoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StorySceneEntityConfiguration : IEntityTypeConfiguration<StorySceneEntity>
{
    public void Configure(EntityTypeBuilder<StorySceneEntity> builder)
    {
        builder.ToTable("scene");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StoryId).HasColumnName("story_id");
        builder.Property(e => e.Key).IsRequired().HasMaxLength(200).HasColumnName("key");
        builder.Property(e => e.Title).HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.HasIndex(e => new { e.StoryId, e.Key }).IsUnique().HasDatabaseName("ix_story_scene_story_key");
        builder.HasMany(e => e.Nodes).WithOne(n => n.Scene).HasForeignKey(n => n.SceneId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class StoryNodeEntityConfiguration : IEntityTypeConfiguration<StoryNodeEntity>
{
    public void Configure(EntityTypeBuilder<StoryNodeEntity> builder)
    {
        builder.ToTable("node");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StoryId).HasColumnName("story_id");
        builder.Property(e => e.SceneId).HasColumnName("scene_id");
        builder.Property(e => e.Key).IsRequired().HasMaxLength(200).HasColumnName("key");
        builder.Property(e => e.Title).HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.IsEnding).HasColumnName("is_ending");
        builder.Property(e => e.PresentationKind).HasConversion<string>().HasColumnName("presentation_kind");
        builder.Property(e => e.Body).HasColumnName("body");
        builder.Property(e => e.ContentType).HasMaxLength(100).HasColumnName("content_type");
        builder.Property(e => e.MediaRef).HasMaxLength(512).HasColumnName("media_ref");
        builder.HasIndex(e => new { e.StoryId, e.Key }).IsUnique().HasDatabaseName("ix_story_node_story_key");
        builder.HasMany(e => e.Choices).WithOne(c => c.Node).HasForeignKey(c => c.NodeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StoryVariableEntityConfiguration : IEntityTypeConfiguration<StoryVariableEntity>
{
    public void Configure(EntityTypeBuilder<StoryVariableEntity> builder)
    {
        builder.ToTable("variable");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StoryId).HasColumnName("story_id");
        builder.Property(e => e.Key).IsRequired().HasMaxLength(200).HasColumnName("key");
        builder.Property(e => e.Name).HasMaxLength(200).HasColumnName("name");
        builder.Property(e => e.Type).HasConversion<string>().HasColumnName("type");
        builder.Property(e => e.DefaultValue).HasMaxLength(2000).HasColumnName("default_value");
        builder.Property(e => e.Description).HasMaxLength(2000).HasColumnName("description");
        builder.HasIndex(e => new { e.StoryId, e.Key }).IsUnique().HasDatabaseName("ix_story_variable_story_key");
    }
}

public sealed class StoryActionEntityConfiguration : IEntityTypeConfiguration<StoryActionEntity>
{
    public void Configure(EntityTypeBuilder<StoryActionEntity> builder)
    {
        builder.ToTable("action");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StoryId).HasColumnName("story_id");
        builder.Property(e => e.Key).IsRequired().HasMaxLength(200).HasColumnName("key");
        builder.Property(e => e.Name).HasMaxLength(200).HasColumnName("name");
        builder.HasIndex(e => new { e.StoryId, e.Key }).IsUnique().HasDatabaseName("ix_story_action_story_key");
        builder.HasMany(e => e.Effects).WithOne(e => e.Action).HasForeignKey(e => e.ActionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StoryChoiceEntityConfiguration : IEntityTypeConfiguration<StoryChoiceEntity>
{
    public void Configure(EntityTypeBuilder<StoryChoiceEntity> builder)
    {
        builder.ToTable("choice");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.NodeId).HasColumnName("node_id");
        builder.Property(e => e.Key).IsRequired().HasMaxLength(200).HasColumnName("key");
        builder.Property(e => e.Prompt).HasMaxLength(2000).HasColumnName("prompt");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.Property(e => e.TargetNodeId).HasColumnName("target_node_id");
        builder.Property(e => e.VisibleWhenJson).HasColumnName("visible_when");
        builder.Property(e => e.EnabledWhenJson).HasColumnName("enabled_when");
        builder.HasIndex(e => new { e.NodeId, e.Key }).IsUnique().HasDatabaseName("ix_story_choice_node_key");
        builder.HasOne(e => e.TargetNode).WithMany().HasForeignKey(e => e.TargetNodeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.Effects).WithOne(e => e.Choice).HasForeignKey(e => e.ChoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StoryReactionEntityConfiguration : IEntityTypeConfiguration<StoryReactionEntity>
{
    public void Configure(EntityTypeBuilder<StoryReactionEntity> builder)
    {
        builder.ToTable("reaction");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StoryId).HasColumnName("story_id");
        builder.Property(e => e.Key).IsRequired().HasMaxLength(200).HasColumnName("key");
        builder.Property(e => e.Trigger).HasConversion<string>().HasColumnName("trigger");
        builder.Property(e => e.ScopeNodeId).HasColumnName("scope_node_id");
        builder.Property(e => e.ScopeVariableKey).HasMaxLength(200).HasColumnName("scope_variable_key");
        builder.Property(e => e.Once).HasColumnName("once");
        builder.Property(e => e.WhenPredicateJson).HasColumnName("when_predicate");
        builder.HasIndex(e => new { e.StoryId, e.Key }).IsUnique().HasDatabaseName("ix_story_reaction_story_key");
        builder.HasOne(e => e.ScopeNode).WithMany().HasForeignKey(e => e.ScopeNodeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.Effects).WithOne(e => e.Reaction).HasForeignKey(e => e.ReactionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StoryEffectEntityConfiguration : IEntityTypeConfiguration<StoryEffectEntity>
{
    public void Configure(EntityTypeBuilder<StoryEffectEntity> builder)
    {
        builder.ToTable("effect", t => t.HasCheckConstraint("ck_story_effect_owner", "((choice_id IS NOT NULL) + (action_id IS NOT NULL) + (reaction_id IS NOT NULL)) = 1"));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ChoiceId).HasColumnName("choice_id");
        builder.Property(e => e.ActionId).HasColumnName("action_id");
        builder.Property(e => e.ReactionId).HasColumnName("reaction_id");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.Property(e => e.Kind).HasConversion<string>().HasColumnName("kind");
        builder.Property(e => e.VariableKey).HasMaxLength(200).HasColumnName("variable_key");
        builder.Property(e => e.Value).HasMaxLength(2000).HasColumnName("value");
        builder.Property(e => e.TargetNodeId).HasColumnName("target_node_id");
        builder.Property(e => e.InvokeActionId).HasColumnName("invoke_action_id");
        builder.HasOne(e => e.TargetNode).WithMany().HasForeignKey(e => e.TargetNodeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.InvokeAction).WithMany().HasForeignKey(e => e.InvokeActionId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class StoryPlaythroughEntityConfiguration : IEntityTypeConfiguration<StoryPlaythroughEntity>
{
    public void Configure(EntityTypeBuilder<StoryPlaythroughEntity> builder)
    {
        builder.ToTable("playthrough");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StoryId).HasColumnName("story_id");
        builder.Property(e => e.PlayerRef).HasMaxLength(500).HasColumnName("player_ref");
        builder.Property(e => e.Status).HasConversion<string>().HasColumnName("status");
        builder.Property(e => e.CurrentNodeId).HasColumnName("current_node_id");
        builder.Property(e => e.StartedAt).IsRequired().HasColumnName("started_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.CompletedAt).HasColumnName("completed_at");
        builder.HasOne(e => e.CurrentNode).WithMany().HasForeignKey(e => e.CurrentNodeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.Variables).WithOne(v => v.Playthrough).HasForeignKey(v => v.PlaythroughId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Events).WithOne(e => e.Playthrough).HasForeignKey(e => e.PlaythroughId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StoryPlaythroughVariableEntityConfiguration : IEntityTypeConfiguration<StoryPlaythroughVariableEntity>
{
    public void Configure(EntityTypeBuilder<StoryPlaythroughVariableEntity> builder)
    {
        builder.ToTable("playthrough_variable");
        builder.HasKey(e => new { e.PlaythroughId, e.VariableKey });
        builder.Property(e => e.PlaythroughId).HasColumnName("playthrough_id");
        builder.Property(e => e.VariableKey).HasMaxLength(200).HasColumnName("variable_key");
        builder.Property(e => e.Value).HasMaxLength(2000).HasColumnName("value");
        builder.Property(e => e.UpdatedAt).IsRequired().HasColumnName("updated_at");
    }
}

public sealed class StoryPlaythroughEventEntityConfiguration : IEntityTypeConfiguration<StoryPlaythroughEventEntity>
{
    public void Configure(EntityTypeBuilder<StoryPlaythroughEventEntity> builder)
    {
        builder.ToTable("playthrough_event");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PlaythroughId).HasColumnName("playthrough_id");
        builder.Property(e => e.Sequence).HasColumnName("sequence");
        builder.Property(e => e.Kind).HasConversion<string>().HasColumnName("kind");
        builder.Property(e => e.NodeId).HasColumnName("node_id");
        builder.Property(e => e.ChoiceId).HasColumnName("choice_id");
        builder.Property(e => e.ReactionId).HasColumnName("reaction_id");
        builder.Property(e => e.ActionId).HasColumnName("action_id");
        builder.Property(e => e.VariableKey).HasMaxLength(200).HasColumnName("variable_key");
        builder.Property(e => e.OldValue).HasMaxLength(2000).HasColumnName("old_value");
        builder.Property(e => e.NewValue).HasMaxLength(2000).HasColumnName("new_value");
        builder.Property(e => e.CausedByEventId).HasColumnName("caused_by_event_id");
        builder.Property(e => e.OccurredAt).IsRequired().HasColumnName("occurred_at");
        builder.HasIndex(e => new { e.PlaythroughId, e.Sequence }).IsUnique().HasDatabaseName("ix_story_playthrough_event_seq");
        builder.HasOne(e => e.Node).WithMany().HasForeignKey(e => e.NodeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Choice).WithMany().HasForeignKey(e => e.ChoiceId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Reaction).WithMany().HasForeignKey(e => e.ReactionId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Action).WithMany().HasForeignKey(e => e.ActionId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.CausedBy).WithMany().HasForeignKey(e => e.CausedByEventId).OnDelete(DeleteBehavior.SetNull);
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Story.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "action",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    story_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "choice",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    node_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    prompt = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    target_node_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    visible_when = table.Column<string>(type: "TEXT", nullable: true),
                    enabled_when = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_choice", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "effect",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    choice_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    action_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    reaction_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    kind = table.Column<string>(type: "TEXT", nullable: false),
                    variable_key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    target_node_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    invoke_action_id = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_effect", x => x.id);
                    table.CheckConstraint("ck_story_effect_owner", "((choice_id IS NOT NULL) + (action_id IS NOT NULL) + (reaction_id IS NOT NULL)) = 1");
                    table.ForeignKey(
                        name: "FK_effect_action_action_id",
                        column: x => x.action_id,
                        principalTable: "action",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_effect_action_invoke_action_id",
                        column: x => x.invoke_action_id,
                        principalTable: "action",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_effect_choice_choice_id",
                        column: x => x.choice_id,
                        principalTable: "choice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    story_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    scene_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    is_ending = table.Column<bool>(type: "INTEGER", nullable: false),
                    presentation_kind = table.Column<string>(type: "TEXT", nullable: false),
                    body = table.Column<string>(type: "TEXT", nullable: true),
                    content_type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    media_ref = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: true),
                    language = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    start_node_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    cover_image_ref = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_story", x => x.id);
                    table.ForeignKey(
                        name: "FK_story_node_start_node_id",
                        column: x => x.start_node_id,
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "playthrough",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    story_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    player_ref = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    current_node_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    started_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playthrough", x => x.id);
                    table.ForeignKey(
                        name: "FK_playthrough_node_current_node_id",
                        column: x => x.current_node_id,
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_playthrough_story_story_id",
                        column: x => x.story_id,
                        principalTable: "story",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reaction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    story_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    trigger = table.Column<string>(type: "TEXT", nullable: false),
                    scope_node_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    scope_variable_key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    once = table.Column<bool>(type: "INTEGER", nullable: false),
                    when_predicate = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reaction", x => x.id);
                    table.ForeignKey(
                        name: "FK_reaction_node_scope_node_id",
                        column: x => x.scope_node_id,
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reaction_story_story_id",
                        column: x => x.story_id,
                        principalTable: "story",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scene",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    story_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scene", x => x.id);
                    table.ForeignKey(
                        name: "FK_scene_story_story_id",
                        column: x => x.story_id,
                        principalTable: "story",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variable",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    story_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    default_value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variable", x => x.id);
                    table.ForeignKey(
                        name: "FK_variable_story_story_id",
                        column: x => x.story_id,
                        principalTable: "story",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playthrough_variable",
                columns: table => new
                {
                    playthrough_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    variable_key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playthrough_variable", x => new { x.playthrough_id, x.variable_key });
                    table.ForeignKey(
                        name: "FK_playthrough_variable_playthrough_playthrough_id",
                        column: x => x.playthrough_id,
                        principalTable: "playthrough",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playthrough_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    playthrough_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    kind = table.Column<string>(type: "TEXT", nullable: false),
                    node_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    choice_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    reaction_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    action_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    variable_key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    old_value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    new_value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    caused_by_event_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playthrough_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_playthrough_event_action_action_id",
                        column: x => x.action_id,
                        principalTable: "action",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_playthrough_event_choice_choice_id",
                        column: x => x.choice_id,
                        principalTable: "choice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_playthrough_event_node_node_id",
                        column: x => x.node_id,
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_playthrough_event_playthrough_event_caused_by_event_id",
                        column: x => x.caused_by_event_id,
                        principalTable: "playthrough_event",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_playthrough_event_playthrough_playthrough_id",
                        column: x => x.playthrough_id,
                        principalTable: "playthrough",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_playthrough_event_reaction_reaction_id",
                        column: x => x.reaction_id,
                        principalTable: "reaction",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_story_action_story_key",
                table: "action",
                columns: new[] { "story_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_choice_target_node_id",
                table: "choice",
                column: "target_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_story_choice_node_key",
                table: "choice",
                columns: new[] { "node_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_effect_action_id",
                table: "effect",
                column: "action_id");

            migrationBuilder.CreateIndex(
                name: "IX_effect_choice_id",
                table: "effect",
                column: "choice_id");

            migrationBuilder.CreateIndex(
                name: "IX_effect_invoke_action_id",
                table: "effect",
                column: "invoke_action_id");

            migrationBuilder.CreateIndex(
                name: "IX_effect_reaction_id",
                table: "effect",
                column: "reaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_effect_target_node_id",
                table: "effect",
                column: "target_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_node_scene_id",
                table: "node",
                column: "scene_id");

            migrationBuilder.CreateIndex(
                name: "ix_story_node_story_key",
                table: "node",
                columns: new[] { "story_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_current_node_id",
                table: "playthrough",
                column: "current_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_story_id",
                table: "playthrough",
                column: "story_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_event_action_id",
                table: "playthrough_event",
                column: "action_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_event_caused_by_event_id",
                table: "playthrough_event",
                column: "caused_by_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_event_choice_id",
                table: "playthrough_event",
                column: "choice_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_event_node_id",
                table: "playthrough_event",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_event_reaction_id",
                table: "playthrough_event",
                column: "reaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_story_playthrough_event_seq",
                table: "playthrough_event",
                columns: new[] { "playthrough_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reaction_scope_node_id",
                table: "reaction",
                column: "scope_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_story_reaction_story_key",
                table: "reaction",
                columns: new[] { "story_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_story_scene_story_key",
                table: "scene",
                columns: new[] { "story_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_story_start_node_id",
                table: "story",
                column: "start_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_story_story_slug",
                table: "story",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_story_variable_story_key",
                table: "variable",
                columns: new[] { "story_id", "key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_action_story_story_id",
                table: "action",
                column: "story_id",
                principalTable: "story",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_choice_node_node_id",
                table: "choice",
                column: "node_id",
                principalTable: "node",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_choice_node_target_node_id",
                table: "choice",
                column: "target_node_id",
                principalTable: "node",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_effect_node_target_node_id",
                table: "effect",
                column: "target_node_id",
                principalTable: "node",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_effect_reaction_reaction_id",
                table: "effect",
                column: "reaction_id",
                principalTable: "reaction",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_node_scene_scene_id",
                table: "node",
                column: "scene_id",
                principalTable: "scene",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_node_story_story_id",
                table: "node",
                column: "story_id",
                principalTable: "story",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_node_story_story_id",
                table: "node");

            migrationBuilder.DropForeignKey(
                name: "FK_scene_story_story_id",
                table: "scene");

            migrationBuilder.DropTable(
                name: "effect");

            migrationBuilder.DropTable(
                name: "playthrough_event");

            migrationBuilder.DropTable(
                name: "playthrough_variable");

            migrationBuilder.DropTable(
                name: "variable");

            migrationBuilder.DropTable(
                name: "action");

            migrationBuilder.DropTable(
                name: "choice");

            migrationBuilder.DropTable(
                name: "reaction");

            migrationBuilder.DropTable(
                name: "playthrough");

            migrationBuilder.DropTable(
                name: "story");

            migrationBuilder.DropTable(
                name: "node");

            migrationBuilder.DropTable(
                name: "scene");
        }
    }
}

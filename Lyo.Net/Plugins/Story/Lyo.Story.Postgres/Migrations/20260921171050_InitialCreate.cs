using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Story.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "story");

            migrationBuilder.CreateTable(
                name: "action",
                schema: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    story_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "choice",
                schema: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    prompt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    target_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visible_when = table.Column<string>(type: "jsonb", nullable: true),
                    enabled_when = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_choice", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "effect",
                schema: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    choice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    variable_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    target_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoke_action_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_effect", x => x.id);
                    table.CheckConstraint("ck_story_effect_owner", "((choice_id IS NOT NULL)::int + (action_id IS NOT NULL)::int + (reaction_id IS NOT NULL)::int) = 1");
                    table.ForeignKey(
                        name: "FK_effect_action_action_id",
                        column: x => x.action_id,
                        principalSchema: "story",
                        principalTable: "action",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_effect_action_invoke_action_id",
                        column: x => x.invoke_action_id,
                        principalSchema: "story",
                        principalTable: "action",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_effect_choice_choice_id",
                        column: x => x.choice_id,
                        principalSchema: "story",
                        principalTable: "choice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node",
                schema: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    story_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_ending = table.Column<bool>(type: "boolean", nullable: false),
                    presentation_kind = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    media_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "story",
                schema: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    start_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cover_image_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_story", x => x.id);
                    table.ForeignKey(
                        name: "FK_story_node_start_node_id",
                        column: x => x.start_node_id,
                        principalSchema: "story",
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "playthrough",
                schema: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    story_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_ref = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    current_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playthrough", x => x.id);
                    table.ForeignKey(
                        name: "FK_playthrough_node_current_node_id",
                        column: x => x.current_node_id,
                        principalSchema: "story",
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_playthrough_story_story_id",
                        column: x => x.story_id,
                        principalSchema: "story",
                        principalTable: "story",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reaction",
                schema: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    story_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trigger = table.Column<string>(type: "text", nullable: false),
                    scope_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scope_variable_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    once = table.Column<bool>(type: "boolean", nullable: false),
                    when_predicate = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reaction", x => x.id);
                    table.ForeignKey(
                        name: "FK_reaction_node_scope_node_id",
                        column: x => x.scope_node_id,
                        principalSchema: "story",
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reaction_story_story_id",
                        column: x => x.story_id,
                        principalSchema: "story",
                        principalTable: "story",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scene",
                schema: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    story_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scene", x => x.id);
                    table.ForeignKey(
                        name: "FK_scene_story_story_id",
                        column: x => x.story_id,
                        principalSchema: "story",
                        principalTable: "story",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variable",
                schema: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    story_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    default_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variable", x => x.id);
                    table.ForeignKey(
                        name: "FK_variable_story_story_id",
                        column: x => x.story_id,
                        principalSchema: "story",
                        principalTable: "story",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playthrough_variable",
                schema: "story",
                columns: table => new
                {
                    playthrough_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variable_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playthrough_variable", x => new { x.playthrough_id, x.variable_key });
                    table.ForeignKey(
                        name: "FK_playthrough_variable_playthrough_playthrough_id",
                        column: x => x.playthrough_id,
                        principalSchema: "story",
                        principalTable: "playthrough",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playthrough_event",
                schema: "story",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    playthrough_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    choice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variable_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    old_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    new_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    caused_by_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playthrough_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_playthrough_event_action_action_id",
                        column: x => x.action_id,
                        principalSchema: "story",
                        principalTable: "action",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_playthrough_event_choice_choice_id",
                        column: x => x.choice_id,
                        principalSchema: "story",
                        principalTable: "choice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_playthrough_event_node_node_id",
                        column: x => x.node_id,
                        principalSchema: "story",
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_playthrough_event_playthrough_event_caused_by_event_id",
                        column: x => x.caused_by_event_id,
                        principalSchema: "story",
                        principalTable: "playthrough_event",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_playthrough_event_playthrough_playthrough_id",
                        column: x => x.playthrough_id,
                        principalSchema: "story",
                        principalTable: "playthrough",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_playthrough_event_reaction_reaction_id",
                        column: x => x.reaction_id,
                        principalSchema: "story",
                        principalTable: "reaction",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_story_action_story_key",
                schema: "story",
                table: "action",
                columns: new[] { "story_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_choice_target_node_id",
                schema: "story",
                table: "choice",
                column: "target_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_story_choice_node_key",
                schema: "story",
                table: "choice",
                columns: new[] { "node_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_effect_action_id",
                schema: "story",
                table: "effect",
                column: "action_id");

            migrationBuilder.CreateIndex(
                name: "IX_effect_choice_id",
                schema: "story",
                table: "effect",
                column: "choice_id");

            migrationBuilder.CreateIndex(
                name: "IX_effect_invoke_action_id",
                schema: "story",
                table: "effect",
                column: "invoke_action_id");

            migrationBuilder.CreateIndex(
                name: "IX_effect_reaction_id",
                schema: "story",
                table: "effect",
                column: "reaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_effect_target_node_id",
                schema: "story",
                table: "effect",
                column: "target_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_node_scene_id",
                schema: "story",
                table: "node",
                column: "scene_id");

            migrationBuilder.CreateIndex(
                name: "ix_story_node_story_key",
                schema: "story",
                table: "node",
                columns: new[] { "story_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_current_node_id",
                schema: "story",
                table: "playthrough",
                column: "current_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_story_id",
                schema: "story",
                table: "playthrough",
                column: "story_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_event_action_id",
                schema: "story",
                table: "playthrough_event",
                column: "action_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_event_caused_by_event_id",
                schema: "story",
                table: "playthrough_event",
                column: "caused_by_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_event_choice_id",
                schema: "story",
                table: "playthrough_event",
                column: "choice_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_event_node_id",
                schema: "story",
                table: "playthrough_event",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "IX_playthrough_event_reaction_id",
                schema: "story",
                table: "playthrough_event",
                column: "reaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_story_playthrough_event_seq",
                schema: "story",
                table: "playthrough_event",
                columns: new[] { "playthrough_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reaction_scope_node_id",
                schema: "story",
                table: "reaction",
                column: "scope_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_story_reaction_story_key",
                schema: "story",
                table: "reaction",
                columns: new[] { "story_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_story_scene_story_key",
                schema: "story",
                table: "scene",
                columns: new[] { "story_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_story_start_node_id",
                schema: "story",
                table: "story",
                column: "start_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_story_story_slug",
                schema: "story",
                table: "story",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_story_variable_story_key",
                schema: "story",
                table: "variable",
                columns: new[] { "story_id", "key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_action_story_story_id",
                schema: "story",
                table: "action",
                column: "story_id",
                principalSchema: "story",
                principalTable: "story",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_choice_node_node_id",
                schema: "story",
                table: "choice",
                column: "node_id",
                principalSchema: "story",
                principalTable: "node",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_choice_node_target_node_id",
                schema: "story",
                table: "choice",
                column: "target_node_id",
                principalSchema: "story",
                principalTable: "node",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_effect_node_target_node_id",
                schema: "story",
                table: "effect",
                column: "target_node_id",
                principalSchema: "story",
                principalTable: "node",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_effect_reaction_reaction_id",
                schema: "story",
                table: "effect",
                column: "reaction_id",
                principalSchema: "story",
                principalTable: "reaction",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_node_scene_scene_id",
                schema: "story",
                table: "node",
                column: "scene_id",
                principalSchema: "story",
                principalTable: "scene",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_node_story_story_id",
                schema: "story",
                table: "node",
                column: "story_id",
                principalSchema: "story",
                principalTable: "story",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_node_story_story_id",
                schema: "story",
                table: "node");

            migrationBuilder.DropForeignKey(
                name: "FK_scene_story_story_id",
                schema: "story",
                table: "scene");

            migrationBuilder.DropTable(
                name: "effect",
                schema: "story");

            migrationBuilder.DropTable(
                name: "playthrough_event",
                schema: "story");

            migrationBuilder.DropTable(
                name: "playthrough_variable",
                schema: "story");

            migrationBuilder.DropTable(
                name: "variable",
                schema: "story");

            migrationBuilder.DropTable(
                name: "action",
                schema: "story");

            migrationBuilder.DropTable(
                name: "choice",
                schema: "story");

            migrationBuilder.DropTable(
                name: "reaction",
                schema: "story");

            migrationBuilder.DropTable(
                name: "playthrough",
                schema: "story");

            migrationBuilder.DropTable(
                name: "story",
                schema: "story");

            migrationBuilder.DropTable(
                name: "node",
                schema: "story");

            migrationBuilder.DropTable(
                name: "scene",
                schema: "story");
        }
    }
}

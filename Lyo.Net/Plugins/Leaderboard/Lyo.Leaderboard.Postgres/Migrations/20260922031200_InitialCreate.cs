using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Leaderboard.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "leaderboard");

            migrationBuilder.CreateTable(
                name: "board",
                schema: "leaderboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    join_policy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    members_can_submit = table.Column<bool>(type: "boolean", nullable: false),
                    require_verification = table.Column<bool>(type: "boolean", nullable: false),
                    members_can_edit_own = table.Column<bool>(type: "boolean", nullable: false),
                    members_can_delete_own = table.Column<bool>(type: "boolean", nullable: false),
                    allow_multiple_entries = table.Column<bool>(type: "boolean", nullable: false),
                    aggregation_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    aggregation_n = table.Column<int>(type: "integer", nullable: true),
                    sort_direction = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tie_break = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tie_break_property_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    algorithm_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invite",
                schema: "leaderboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    share_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    invited_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    short_url_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    max_uses = table.Column<int>(type: "integer", nullable: false),
                    use_count = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invite", x => x.id);
                    table.ForeignKey(
                        name: "FK_invite_board_board_id",
                        column: x => x.board_id,
                        principalSchema: "leaderboard",
                        principalTable: "board",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "member",
                schema: "leaderboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member", x => x.id);
                    table.ForeignKey(
                        name: "FK_member_board_board_id",
                        column: x => x.board_id,
                        principalSchema: "leaderboard",
                        principalTable: "board",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scoring_property",
                schema: "leaderboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    value_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    min_value = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    max_value = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    validation_regex = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    choices_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scoring_property", x => x.id);
                    table.ForeignKey(
                        name: "FK_scoring_property_board_board_id",
                        column: x => x.board_id,
                        principalSchema: "leaderboard",
                        principalTable: "board",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "adjustment",
                schema: "leaderboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delta = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adjustment", x => x.id);
                    table.ForeignKey(
                        name: "FK_adjustment_board_board_id",
                        column: x => x.board_id,
                        principalSchema: "leaderboard",
                        principalTable: "board",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_adjustment_member_member_id",
                        column: x => x.member_id,
                        principalSchema: "leaderboard",
                        principalTable: "member",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entry",
                schema: "leaderboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    computed_score = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    score_breakdown_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entry", x => x.id);
                    table.ForeignKey(
                        name: "FK_entry_board_board_id",
                        column: x => x.board_id,
                        principalSchema: "leaderboard",
                        principalTable: "board",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_entry_member_member_id",
                        column: x => x.member_id,
                        principalSchema: "leaderboard",
                        principalTable: "member",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "standing",
                schema: "leaderboard",
                columns: table => new
                {
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_score = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    adjustment_score = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_score = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    entry_count = table.Column<int>(type: "integer", nullable: false),
                    last_entry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_standing", x => new { x.board_id, x.member_id });
                    table.ForeignKey(
                        name: "FK_standing_board_board_id",
                        column: x => x.board_id,
                        principalSchema: "leaderboard",
                        principalTable: "board",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_standing_member_member_id",
                        column: x => x.member_id,
                        principalSchema: "leaderboard",
                        principalTable: "member",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entry_value",
                schema: "leaderboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    choice_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    number_value = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    text_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    bool_value = table.Column<bool>(type: "boolean", nullable: true),
                    datetime_value = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entry_value", x => x.id);
                    table.ForeignKey(
                        name: "FK_entry_value_entry_entry_id",
                        column: x => x.entry_id,
                        principalSchema: "leaderboard",
                        principalTable: "entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_entry_value_scoring_property_property_id",
                        column: x => x.property_id,
                        principalSchema: "leaderboard",
                        principalTable: "scoring_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adjustment_board_id",
                schema: "leaderboard",
                table: "adjustment",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "IX_adjustment_member_id",
                schema: "leaderboard",
                table: "adjustment",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_board_owner",
                schema: "leaderboard",
                table: "board",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_leaderboard_board_slug",
                schema: "leaderboard",
                table: "board",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entry_member_id",
                schema: "leaderboard",
                table: "entry",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_entry_board_member_status",
                schema: "leaderboard",
                table: "entry",
                columns: new[] { "board_id", "member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_entry_board_status_score",
                schema: "leaderboard",
                table: "entry",
                columns: new[] { "board_id", "status", "computed_score" });

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_entry_value_property_number",
                schema: "leaderboard",
                table: "entry_value",
                columns: new[] { "property_id", "number_value" });

            migrationBuilder.CreateIndex(
                name: "ux_leaderboard_entry_value_entry_property",
                schema: "leaderboard",
                table: "entry_value",
                columns: new[] { "entry_id", "property_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invite_board_id",
                schema: "leaderboard",
                table: "invite",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "ux_leaderboard_invite_token_hash",
                schema: "leaderboard",
                table: "invite",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_member_board_status",
                schema: "leaderboard",
                table: "member",
                columns: new[] { "board_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_leaderboard_member_board_user",
                schema: "leaderboard",
                table: "member",
                columns: new[] { "board_id", "user_id" },
                unique: true,
                filter: "\"user_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_leaderboard_property_board_key",
                schema: "leaderboard",
                table: "scoring_property",
                columns: new[] { "board_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_standing_board_score",
                schema: "leaderboard",
                table: "standing",
                columns: new[] { "board_id", "total_score" });

            migrationBuilder.CreateIndex(
                name: "IX_standing_member_id",
                schema: "leaderboard",
                table: "standing",
                column: "member_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adjustment",
                schema: "leaderboard");

            migrationBuilder.DropTable(
                name: "entry_value",
                schema: "leaderboard");

            migrationBuilder.DropTable(
                name: "invite",
                schema: "leaderboard");

            migrationBuilder.DropTable(
                name: "standing",
                schema: "leaderboard");

            migrationBuilder.DropTable(
                name: "entry",
                schema: "leaderboard");

            migrationBuilder.DropTable(
                name: "scoring_property",
                schema: "leaderboard");

            migrationBuilder.DropTable(
                name: "member",
                schema: "leaderboard");

            migrationBuilder.DropTable(
                name: "board",
                schema: "leaderboard");
        }
    }
}

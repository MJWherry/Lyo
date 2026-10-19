using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.FantasyFootball.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fantasy_football");

            migrationBuilder.CreateTable(
                name: "league",
                schema: "fantasy_football",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    espn_league_id = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    scoring_period_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conversation",
                schema: "fantasy_football",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espn_topic_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    unread_count = table.Column<int>(type: "integer", nullable: false),
                    total_message_count = table.Column<int>(type: "integer", nullable: false),
                    subject = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation", x => x.id);
                    table.ForeignKey(
                        name: "FK_conversation_league_league_id",
                        column: x => x.league_id,
                        principalSchema: "fantasy_football",
                        principalTable: "league",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "member",
                schema: "fantasy_football",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espn_member_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member", x => x.id);
                    table.ForeignKey(
                        name: "FK_member_league_league_id",
                        column: x => x.league_id,
                        principalSchema: "fantasy_football",
                        principalTable: "league",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team",
                schema: "fantasy_football",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espn_team_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    abbrev = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    nickname = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    primary_owner_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_league_league_id",
                        column: x => x.league_id,
                        principalSchema: "fantasy_football",
                        principalTable: "league",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trade",
                schema: "fantasy_football",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espn_transaction_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    scoring_period_id = table.Column<int>(type: "integer", nullable: true),
                    is_pending = table.Column<bool>(type: "boolean", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trade", x => x.id);
                    table.ForeignKey(
                        name: "FK_trade_league_league_id",
                        column: x => x.league_id,
                        principalSchema: "fantasy_football",
                        principalTable: "league",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message",
                schema: "fantasy_football",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espn_message_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    author_espn_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    author_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    espn_transaction_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message", x => x.id);
                    table.ForeignKey(
                        name: "FK_message_conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "fantasy_football",
                        principalTable: "conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversation_member",
                schema: "fantasy_football",
                columns: table => new
                {
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_member", x => new { x.conversation_id, x.member_id });
                    table.ForeignKey(
                        name: "FK_conversation_member_conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "fantasy_football",
                        principalTable: "conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_conversation_member_member_member_id",
                        column: x => x.member_id,
                        principalSchema: "fantasy_football",
                        principalTable: "member",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roster_entry",
                schema: "fantasy_football",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scoring_period_id = table.Column<int>(type: "integer", nullable: false),
                    player_id = table.Column<int>(type: "integer", nullable: false),
                    player_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roster_entry", x => x.id);
                    table.ForeignKey(
                        name: "FK_roster_entry_team_team_id",
                        column: x => x.team_id,
                        principalSchema: "fantasy_football",
                        principalTable: "team",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trade_item",
                schema: "fantasy_football",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_espn_team_id = table.Column<int>(type: "integer", nullable: true),
                    to_espn_team_id = table.Column<int>(type: "integer", nullable: true),
                    player_id = table.Column<int>(type: "integer", nullable: true),
                    player_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trade_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_trade_item_trade_trade_id",
                        column: x => x.trade_id,
                        principalSchema: "fantasy_football",
                        principalTable: "trade",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_player",
                schema: "fantasy_football",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    espn_team_id = table.Column<int>(type: "integer", nullable: true),
                    player_id = table.Column<int>(type: "integer", nullable: false),
                    player_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    direction = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_player", x => x.id);
                    table.ForeignKey(
                        name: "FK_message_player_message_message_id",
                        column: x => x.message_id,
                        principalSchema: "fantasy_football",
                        principalTable: "message",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_fantasy_football_conversation_topic",
                schema: "fantasy_football",
                table: "conversation",
                columns: new[] { "league_id", "espn_topic_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversation_member_member_id",
                schema: "fantasy_football",
                table: "conversation_member",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ux_fantasy_football_league_espn",
                schema: "fantasy_football",
                table: "league",
                columns: new[] { "espn_league_id", "season_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fantasy_football_member_espn",
                schema: "fantasy_football",
                table: "member",
                columns: new[] { "league_id", "espn_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fantasy_football_message_sent",
                schema: "fantasy_football",
                table: "message",
                columns: new[] { "conversation_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ux_fantasy_football_message_espn",
                schema: "fantasy_football",
                table: "message",
                columns: new[] { "conversation_id", "espn_message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_message_player_message_id",
                schema: "fantasy_football",
                table: "message_player",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ux_fantasy_football_roster_player",
                schema: "fantasy_football",
                table: "roster_entry",
                columns: new[] { "team_id", "scoring_period_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fantasy_football_team_espn",
                schema: "fantasy_football",
                table: "team",
                columns: new[] { "league_id", "espn_team_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fantasy_football_trade_espn",
                schema: "fantasy_football",
                table: "trade",
                columns: new[] { "league_id", "espn_transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trade_item_trade_id",
                schema: "fantasy_football",
                table: "trade_item",
                column: "trade_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_member",
                schema: "fantasy_football");

            migrationBuilder.DropTable(
                name: "message_player",
                schema: "fantasy_football");

            migrationBuilder.DropTable(
                name: "roster_entry",
                schema: "fantasy_football");

            migrationBuilder.DropTable(
                name: "trade_item",
                schema: "fantasy_football");

            migrationBuilder.DropTable(
                name: "member",
                schema: "fantasy_football");

            migrationBuilder.DropTable(
                name: "message",
                schema: "fantasy_football");

            migrationBuilder.DropTable(
                name: "team",
                schema: "fantasy_football");

            migrationBuilder.DropTable(
                name: "trade",
                schema: "fantasy_football");

            migrationBuilder.DropTable(
                name: "conversation",
                schema: "fantasy_football");

            migrationBuilder.DropTable(
                name: "league",
                schema: "fantasy_football");
        }
    }
}

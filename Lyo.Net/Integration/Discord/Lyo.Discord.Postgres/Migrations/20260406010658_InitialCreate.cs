using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lyo.Discord.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "discord");

            migrationBuilder.CreateTable(
                name: "discord_user",
                schema: "discord",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    username = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    discriminator = table.Column<int>(type: "integer", nullable: false),
                    email = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    locale = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: true),
                    is_bot = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: true),
                    is_mfa_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    premium_level = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    user_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discord_guild",
                schema: "discord",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    member_count = table.Column<int>(type: "integer", nullable: false),
                    current_subscription_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_large = table.Column<bool>(type: "boolean", nullable: false),
                    is_nsfw = table.Column<bool>(type: "boolean", nullable: false),
                    is_unavailable = table.Column<bool>(type: "boolean", nullable: false),
                    guild_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    joined_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_guild", x => x.id);
                    table.ForeignKey(
                        name: "fk_discord_guild_owner_discord_user",
                        column: x => x.owner_id,
                        principalSchema: "discord",
                        principalTable: "discord_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discord_channel",
                schema: "discord",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    topic = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_category = table.Column<bool>(type: "boolean", nullable: false),
                    is_nsfw = table.Column<bool>(type: "boolean", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    is_thread = table.Column<bool>(type: "boolean", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    channel_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_channel", x => x.id);
                    table.ForeignKey(
                        name: "fk_discord_channel_guild",
                        column: x => x.guild_id,
                        principalSchema: "discord",
                        principalTable: "discord_guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discord_emoji",
                schema: "discord",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: true),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_animated = table.Column<bool>(type: "boolean", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    is_managed = table.Column<bool>(type: "boolean", nullable: false),
                    requires_colons = table.Column<bool>(type: "boolean", nullable: false),
                    emoji_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_emoji", x => x.id);
                    table.ForeignKey(
                        name: "fk_discord_emoji_guild",
                        column: x => x.guild_id,
                        principalSchema: "discord",
                        principalTable: "discord_guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discord_member",
                schema: "discord",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    joined_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    nickname = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    extra_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_member", x => new { x.user_id, x.guild_id });
                    table.ForeignKey(
                        name: "fk_discord_member_guild",
                        column: x => x.guild_id,
                        principalSchema: "discord",
                        principalTable: "discord_guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_discord_member_user",
                        column: x => x.user_id,
                        principalSchema: "discord",
                        principalTable: "discord_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "discord_interaction",
                schema: "discord",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    channel_id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    interaction_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_interaction", x => x.id);
                    table.ForeignKey(
                        name: "fk_discord_interaction_author",
                        column: x => x.author_id,
                        principalSchema: "discord",
                        principalTable: "discord_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discord_interaction_channel",
                        column: x => x.channel_id,
                        principalSchema: "discord",
                        principalTable: "discord_channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discord_interaction_guild",
                        column: x => x.guild_id,
                        principalSchema: "discord",
                        principalTable: "discord_guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discord_message",
                schema: "discord",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    channel_id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    message_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_message", x => x.id);
                    table.ForeignKey(
                        name: "fk_discord_message_author",
                        column: x => x.author_id,
                        principalSchema: "discord",
                        principalTable: "discord_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discord_message_channel",
                        column: x => x.channel_id,
                        principalSchema: "discord",
                        principalTable: "discord_channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discord_message_guild",
                        column: x => x.guild_id,
                        principalSchema: "discord",
                        principalTable: "discord_guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discord_role",
                schema: "discord",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    emoji_id = table.Column<long>(type: "bigint", nullable: true),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    icon = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    is_hoisted = table.Column<bool>(type: "boolean", nullable: false),
                    is_managed = table.Column<bool>(type: "boolean", nullable: false),
                    is_mentionable = table.Column<bool>(type: "boolean", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    role_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_discord_role_emoji",
                        column: x => x.emoji_id,
                        principalSchema: "discord",
                        principalTable: "discord_emoji",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discord_role_guild",
                        column: x => x.guild_id,
                        principalSchema: "discord",
                        principalTable: "discord_guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discord_attachment",
                schema: "discord",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    interaction_id = table.Column<long>(type: "bigint", nullable: true),
                    message_id = table.Column<long>(type: "bigint", nullable: true),
                    filename = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_size = table.Column<int>(type: "integer", nullable: false),
                    media_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    proxy_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    attachment_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_attachment", x => x.id);
                    table.ForeignKey(
                        name: "fk_discord_attachment_interaction",
                        column: x => x.interaction_id,
                        principalSchema: "discord",
                        principalTable: "discord_interaction",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discord_attachment_message",
                        column: x => x.message_id,
                        principalSchema: "discord",
                        principalTable: "discord_message",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discord_reactions",
                schema: "discord",
                columns: table => new
                {
                    message_id = table.Column<long>(type: "bigint", nullable: false),
                    reactor_id = table.Column<long>(type: "bigint", nullable: false),
                    emoji_id = table.Column<long>(type: "bigint", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_reactions", x => new { x.message_id, x.reactor_id, x.emoji_id });
                    table.ForeignKey(
                        name: "fk_discord_reactions_emoji",
                        column: x => x.emoji_id,
                        principalSchema: "discord",
                        principalTable: "discord_emoji",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discord_reactions_message",
                        column: x => x.message_id,
                        principalSchema: "discord",
                        principalTable: "discord_message",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_discord_reactions_reactor",
                        column: x => x.reactor_id,
                        principalSchema: "discord",
                        principalTable: "discord_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_discord_attachment_interaction_id",
                schema: "discord",
                table: "discord_attachment",
                column: "interaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_attachment_message_id",
                schema: "discord",
                table: "discord_attachment",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_channel_guild_id",
                schema: "discord",
                table: "discord_channel",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_emoji_guild_id",
                schema: "discord",
                table: "discord_emoji",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "IX_discord_guild_owner_id",
                schema: "discord",
                table: "discord_guild",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_discord_interaction_author_id",
                schema: "discord",
                table: "discord_interaction",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_interaction_channel_id",
                schema: "discord",
                table: "discord_interaction",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_interaction_guild_id",
                schema: "discord",
                table: "discord_interaction",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "IX_discord_message_author_id",
                schema: "discord",
                table: "discord_message",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_message_channel_id",
                schema: "discord",
                table: "discord_message",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_message_guild_id",
                schema: "discord",
                table: "discord_message",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "IX_discord_reactions_emoji_id",
                schema: "discord",
                table: "discord_reactions",
                column: "emoji_id");

            migrationBuilder.CreateIndex(
                name: "IX_discord_reactions_reactor_id",
                schema: "discord",
                table: "discord_reactions",
                column: "reactor_id");

            migrationBuilder.CreateIndex(
                name: "IX_discord_role_emoji_id",
                schema: "discord",
                table: "discord_role",
                column: "emoji_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_role_guild_id",
                schema: "discord",
                table: "discord_role",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_member_guild_id",
                schema: "discord",
                table: "discord_member",
                column: "guild_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discord_attachment",
                schema: "discord");

            migrationBuilder.DropTable(
                name: "discord_reactions",
                schema: "discord");

            migrationBuilder.DropTable(
                name: "discord_role",
                schema: "discord");

            migrationBuilder.DropTable(
                name: "discord_member",
                schema: "discord");

            migrationBuilder.DropTable(
                name: "discord_interaction",
                schema: "discord");

            migrationBuilder.DropTable(
                name: "discord_message",
                schema: "discord");

            migrationBuilder.DropTable(
                name: "discord_emoji",
                schema: "discord");

            migrationBuilder.DropTable(
                name: "discord_channel",
                schema: "discord");

            migrationBuilder.DropTable(
                name: "discord_guild",
                schema: "discord");

            migrationBuilder.DropTable(
                name: "discord_user",
                schema: "discord");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronofoil.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CfTokens",
                columns: table => new
                {
                    TokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RemoteTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfTokens", x => x.TokenId);
                });

            migrationBuilder.CreateTable(
                name: "Opcodes",
                columns: table => new
                {
                    GameVersion = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Opcode = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opcodes", x => new { x.GameVersion, x.Key });
                });

            migrationBuilder.CreateTable(
                name: "RemoteTokens",
                columns: table => new
                {
                    TokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ProviderUserId = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteTokens", x => x.TokenId);
                });

            migrationBuilder.CreateTable(
                name: "Uploads",
                columns: table => new
                {
                    CfCaptureId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MetricTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MetricWhenEos = table.Column<bool>(type: "boolean", nullable: false),
                    PublicTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublicWhenEos = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uploads", x => x.CfCaptureId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    CfUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TosVersion = table.Column<int>(type: "integer", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    IsBanned = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.CfUserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CfTokens_RefreshToken",
                table: "CfTokens",
                column: "RefreshToken");

            migrationBuilder.CreateIndex(
                name: "IX_RemoteTokens_UserId",
                table: "RemoteTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CfTokens");

            migrationBuilder.DropTable(
                name: "Opcodes");

            migrationBuilder.DropTable(
                name: "RemoteTokens");

            migrationBuilder.DropTable(
                name: "Uploads");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

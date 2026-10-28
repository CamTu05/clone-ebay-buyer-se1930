using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameChangeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.AddColumn<DateTime>(
                name: "lastUsernameChangedAt",
                table: "User",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalizedUsername",
                table: "User",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                computedColumnSql: "UPPER(LTRIM(RTRIM([username])))",
                stored: true);

            migrationBuilder.CreateTable(
                name: "UsernameChangeHistory",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: false),
                    oldUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    newUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    changedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    oldUsernameReservedUntilUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    normalizedOldUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, computedColumnSql: "UPPER(LTRIM(RTRIM([oldUsername])))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsernameChangeHistory", x => x.id);
                    table.ForeignKey(
                        name: "FK_UsernameChangeHistory_User",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "UX_User_NormalizedUsername",
                table: "User",
                column: "normalizedUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsernameHistory_OldName_ReservedUntil",
                schema: "dbo",
                table: "UsernameChangeHistory",
                columns: new[] { "normalizedOldUsername", "oldUsernameReservedUntilUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UsernameHistory_User_ChangedAt",
                schema: "dbo",
                table: "UsernameChangeHistory",
                columns: new[] { "userId", "changedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsernameChangeHistory",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "UX_User_NormalizedUsername",
                table: "User");

            migrationBuilder.DropColumn(
                name: "normalizedUsername",
                table: "User");

            migrationBuilder.DropColumn(
                name: "lastUsernameChangedAt",
                table: "User");
        }
    }
}

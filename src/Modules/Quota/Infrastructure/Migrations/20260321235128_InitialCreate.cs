using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicGrabber.Modules.Quota.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    QuotaBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    UsedBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    FileCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentThreshold = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LastEmailSentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastEmailThreshold = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuotas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserQuotas_UserId",
                table: "UserQuotas",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserQuotas");
        }
    }
}

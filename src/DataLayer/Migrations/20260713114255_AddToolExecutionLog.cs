using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fistix.TaskManager.DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddToolExecutionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToolExecutionLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ToolName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Parameters = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolExecutionLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutionLog_ExecutedAt",
                table: "ToolExecutionLog",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutionLog_ToolName",
                table: "ToolExecutionLog",
                column: "ToolName");

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutionLog_UserId",
                table: "ToolExecutionLog",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToolExecutionLog");
        }
    }
}

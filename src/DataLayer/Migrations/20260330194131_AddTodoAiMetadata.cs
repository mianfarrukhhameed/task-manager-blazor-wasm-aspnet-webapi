using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fistix.TaskManager.DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoAiMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TodoAiMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TodoId = table.Column<int>(type: "int", nullable: false),
                    AiSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiPriority = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceScore = table.Column<float>(type: "real", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoAiMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoAiMetadata_TodoTask_TodoId",
                        column: x => x.TodoId,
                        principalTable: "TodoTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoAiMetadata_TodoId",
                table: "TodoAiMetadata",
                column: "TodoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TodoAiMetadata");
        }
    }
}

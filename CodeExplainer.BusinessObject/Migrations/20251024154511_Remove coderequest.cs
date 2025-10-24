using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeExplainer.BusinessObject.Migrations
{
    /// <inheritdoc />
    public partial class Removecoderequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeRequests",
                columns: table => new
                {
                    CodeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AIResponse = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    PromptType = table.Column<string>(type: "text", nullable: false),
                    SourceCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeRequests", x => x.CodeRequestId);
                    table.ForeignKey(
                        name: "FK_CodeRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeRequests_UserId",
                table: "CodeRequests",
                column: "UserId");
        }
    }
}

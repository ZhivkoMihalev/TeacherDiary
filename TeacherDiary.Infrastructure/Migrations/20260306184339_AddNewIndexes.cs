using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_ActivityType",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_Date",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_StudentProfileId",
                table: "ActivityLogs");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_TeacherId_OrganizationId",
                table: "Classes",
                columns: new[] { "TeacherId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_StudentProfileId_ActivityType",
                table: "ActivityLogs",
                columns: new[] { "StudentProfileId", "ActivityType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Classes_TeacherId_OrganizationId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_StudentProfileId_ActivityType",
                table: "ActivityLogs");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_ActivityType",
                table: "ActivityLogs",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Date",
                table: "ActivityLogs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_StudentProfileId",
                table: "ActivityLogs",
                column: "StudentProfileId");
        }
    }
}

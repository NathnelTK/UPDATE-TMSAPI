using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Approval workflow: add the Status column to Enrollments.
            // Stored as int (EnrollmentStatus enum) with a DB default of 0 (Pending),
            // so any rows created before this column existed read back as Pending.
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Enrollments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Enrollments");
        }
    }
}

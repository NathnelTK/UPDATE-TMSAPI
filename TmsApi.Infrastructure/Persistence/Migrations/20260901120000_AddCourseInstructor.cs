using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseInstructor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // M11 Session 3 - Exercise 5: lead instructor for resource-based authorization.
            // Nullable text column so existing courses (which have no assigned instructor)
            // migrate cleanly; CourseInstructorHandler compares it to the caller's user id
            // to decide whether an Instructor may edit the course.
            migrationBuilder.AddColumn<string>(
                name: "InstructorId",
                table: "Courses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Courses");
        }
    }
}

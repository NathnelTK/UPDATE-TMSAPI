using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCatalogDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Courses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "General Skills");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Courses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "Course information will be updated by the training office.");

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Courses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "8 weeks");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Courses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EnrollmentEndDate",
                table: "Courses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EnrollmentStartDate",
                table: "Courses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MinimumRequirements",
                table: "Courses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "Basic computer literacy");

            migrationBuilder.AddColumn<string>(
                name: "Prerequisites",
                table: "Courses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Courses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Courses",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Published");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EnrollmentEndDate",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EnrollmentStartDate",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "MinimumRequirements",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Prerequisites",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");
        }
    }
}

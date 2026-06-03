using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLTTYKPH.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Surveys",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Surveys",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Surveys");
        }
    }
}

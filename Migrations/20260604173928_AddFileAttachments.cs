using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLTTYKPH.Migrations
{
    /// <inheritdoc />
    public partial class AddFileAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "Surveys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "Feedbacks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionAttachmentPath",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ResolutionAttachmentPath",
                table: "Complaints");
        }
    }
}

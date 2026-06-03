using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLTTYKPH.Migrations
{
    /// <inheritdoc />
    public partial class AddAnonymousComplaint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "Complaints",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "Complaints");
        }
    }
}

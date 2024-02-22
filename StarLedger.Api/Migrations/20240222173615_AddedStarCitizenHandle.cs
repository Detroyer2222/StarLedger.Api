using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarLedger.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddedStarCitizenHandle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StarCitizenHandle",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StarCitizenHandle",
                table: "AspNetUsers");
        }
    }
}

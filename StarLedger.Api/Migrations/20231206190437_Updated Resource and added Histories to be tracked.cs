using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarLedger.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedResourceandaddedHistoriestobetracked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResourceQuantityHistory_AspNetUsers_UserId",
                table: "ResourceQuantityHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceQuantityHistory_Resources_ResourceId",
                table: "ResourceQuantityHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBalanceHistory_AspNetUsers_UserId",
                table: "UserBalanceHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserBalanceHistory",
                table: "UserBalanceHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResourceQuantityHistory",
                table: "ResourceQuantityHistory");

            migrationBuilder.RenameTable(
                name: "UserBalanceHistory",
                newName: "UserBalanceHistories");

            migrationBuilder.RenameTable(
                name: "ResourceQuantityHistory",
                newName: "ResourceQuantityHistories");

            migrationBuilder.RenameIndex(
                name: "IX_UserBalanceHistory_UserId",
                table: "UserBalanceHistories",
                newName: "IX_UserBalanceHistories_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceQuantityHistory_UserId",
                table: "ResourceQuantityHistories",
                newName: "IX_ResourceQuantityHistories_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceQuantityHistory_ResourceId",
                table: "ResourceQuantityHistories",
                newName: "IX_ResourceQuantityHistories_ResourceId");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Resources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserBalanceHistories",
                table: "UserBalanceHistories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResourceQuantityHistories",
                table: "ResourceQuantityHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceQuantityHistories_AspNetUsers_UserId",
                table: "ResourceQuantityHistories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceQuantityHistories_Resources_ResourceId",
                table: "ResourceQuantityHistories",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "ResourceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBalanceHistories_AspNetUsers_UserId",
                table: "UserBalanceHistories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResourceQuantityHistories_AspNetUsers_UserId",
                table: "ResourceQuantityHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceQuantityHistories_Resources_ResourceId",
                table: "ResourceQuantityHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBalanceHistories_AspNetUsers_UserId",
                table: "UserBalanceHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserBalanceHistories",
                table: "UserBalanceHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResourceQuantityHistories",
                table: "ResourceQuantityHistories");

            migrationBuilder.RenameTable(
                name: "UserBalanceHistories",
                newName: "UserBalanceHistory");

            migrationBuilder.RenameTable(
                name: "ResourceQuantityHistories",
                newName: "ResourceQuantityHistory");

            migrationBuilder.RenameIndex(
                name: "IX_UserBalanceHistories_UserId",
                table: "UserBalanceHistory",
                newName: "IX_UserBalanceHistory_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceQuantityHistories_UserId",
                table: "ResourceQuantityHistory",
                newName: "IX_ResourceQuantityHistory_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceQuantityHistories_ResourceId",
                table: "ResourceQuantityHistory",
                newName: "IX_ResourceQuantityHistory_ResourceId");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Resources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserBalanceHistory",
                table: "UserBalanceHistory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResourceQuantityHistory",
                table: "ResourceQuantityHistory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceQuantityHistory_AspNetUsers_UserId",
                table: "ResourceQuantityHistory",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceQuantityHistory_Resources_ResourceId",
                table: "ResourceQuantityHistory",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "ResourceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBalanceHistory_AspNetUsers_UserId",
                table: "UserBalanceHistory",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

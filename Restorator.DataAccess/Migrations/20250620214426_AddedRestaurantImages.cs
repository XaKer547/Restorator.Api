using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restorator.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedRestaurantImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantImage_Restaurants_RestaurantId",
                table: "RestaurantImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RestaurantImage",
                table: "RestaurantImage");

            migrationBuilder.RenameTable(
                name: "RestaurantImage",
                newName: "RestaurantImages");

            migrationBuilder.RenameIndex(
                name: "IX_RestaurantImage_RestaurantId",
                table: "RestaurantImages",
                newName: "IX_RestaurantImages_RestaurantId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RestaurantImages",
                table: "RestaurantImages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantImages_Restaurants_RestaurantId",
                table: "RestaurantImages",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantImages_Restaurants_RestaurantId",
                table: "RestaurantImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RestaurantImages",
                table: "RestaurantImages");

            migrationBuilder.RenameTable(
                name: "RestaurantImages",
                newName: "RestaurantImage");

            migrationBuilder.RenameIndex(
                name: "IX_RestaurantImages_RestaurantId",
                table: "RestaurantImage",
                newName: "IX_RestaurantImage_RestaurantId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RestaurantImage",
                table: "RestaurantImage",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantImage_Restaurants_RestaurantId",
                table: "RestaurantImage",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id");
        }
    }
}

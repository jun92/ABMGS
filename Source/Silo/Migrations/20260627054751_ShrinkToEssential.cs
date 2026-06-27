using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Silo.Migrations
{
    /// <inheritdoc />
    public partial class ShrinkToEssential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosX",
                table: "player_data");

            migrationBuilder.DropColumn(
                name: "PosY",
                table: "player_data");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "player_data");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "player_data",
                newName: "CustomLevel");

            migrationBuilder.RenameColumn(
                name: "Exp",
                table: "player_data",
                newName: "CustomExp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomLevel",
                table: "player_data",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "CustomExp",
                table: "player_data",
                newName: "Exp");

            migrationBuilder.AddColumn<float>(
                name: "PosX",
                table: "player_data",
                type: "real",
                nullable: false,
                defaultValue: 10f);

            migrationBuilder.AddColumn<float>(
                name: "PosY",
                table: "player_data",
                type: "real",
                nullable: false,
                defaultValue: -10f);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "player_data",
                type: "text",
                nullable: true,
                defaultValue: "No Title");
        }
    }
}

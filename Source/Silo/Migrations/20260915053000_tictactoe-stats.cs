using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Silo.Migrations
{
    /// <inheritdoc />
    public partial class tictactoestats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomExp",
                table: "player_data");

            migrationBuilder.DropColumn(
                name: "CustomLevel",
                table: "player_data");

            migrationBuilder.AddColumn<int>(
                name: "LoseCount",
                table: "player_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlayCount",
                table: "player_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WinCount",
                table: "player_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoseCount",
                table: "player_data");

            migrationBuilder.DropColumn(
                name: "PlayCount",
                table: "player_data");

            migrationBuilder.DropColumn(
                name: "WinCount",
                table: "player_data");

            migrationBuilder.AddColumn<long>(
                name: "CustomExp",
                table: "player_data",
                type: "bigint",
                nullable: false,
                defaultValue: 33L);

            migrationBuilder.AddColumn<int>(
                name: "CustomLevel",
                table: "player_data",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }
    }
}

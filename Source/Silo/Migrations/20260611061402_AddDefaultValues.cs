using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Silo.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "player_data",
                type: "text",
                nullable: true,
                defaultValue: "No Title",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "PosY",
                table: "player_data",
                type: "real",
                nullable: false,
                defaultValue: -10f,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "PosX",
                table: "player_data",
                type: "real",
                nullable: false,
                defaultValue: 10f,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<int>(
                name: "Level",
                table: "player_data",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "player_data",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldDefaultValue: "No Title");

            migrationBuilder.AlterColumn<float>(
                name: "PosY",
                table: "player_data",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real",
                oldDefaultValue: -10f);

            migrationBuilder.AlterColumn<float>(
                name: "PosX",
                table: "player_data",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real",
                oldDefaultValue: 10f);

            migrationBuilder.AlterColumn<int>(
                name: "Level",
                table: "player_data",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);
        }
    }
}

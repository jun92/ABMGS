using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Silo.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDefaultVar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "CustomExp",
                table: "player_data",
                type: "bigint",
                nullable: false,
                defaultValue: 33L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "CustomExp",
                table: "player_data",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 33L);
        }
    }
}

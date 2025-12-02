using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABMGS.ServerV2.Silo.Migrations
{
    /// <inheritdoc />
    public partial class RemoveName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "players_extend");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "players_extend",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

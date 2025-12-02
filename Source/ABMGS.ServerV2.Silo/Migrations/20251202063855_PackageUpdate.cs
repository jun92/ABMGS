using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ABMGS.ServerV2.Silo.Migrations
{
    /// <inheritdoc />
    public partial class PackageUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Introduction",
                table: "players");

            migrationBuilder.CreateTable(
                name: "id_provider_mapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderId = table.Column<string>(type: "text", nullable: false),
                    SyncnetPlayerId = table.Column<int>(type: "integer", nullable: false),
                    IdProviderType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_id_provider_mapping", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_id_provider_mapping_ProviderId_SyncnetPlayerId",
                table: "id_provider_mapping",
                columns: new[] { "ProviderId", "SyncnetPlayerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "id_provider_mapping");

            migrationBuilder.AddColumn<string>(
                name: "Introduction",
                table: "players",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

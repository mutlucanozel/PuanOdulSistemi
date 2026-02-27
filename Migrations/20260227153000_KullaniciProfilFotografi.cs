using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuanOdulSistemi.Migrations
{
    /// <inheritdoc />
    public partial class KullaniciProfilFotografi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilFotografYolu",
                table: "Kullanicilar",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilFotografYolu",
                table: "Kullanicilar");
        }
    }
}

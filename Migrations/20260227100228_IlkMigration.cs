using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PuanOdulSistemi.Migrations
{
    /// <inheritdoc />
    public partial class IlkMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "TEXT", nullable: false),
                    Sifre = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Oduller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", nullable: true),
                    GerekliPuan = table.Column<int>(type: "INTEGER", nullable: false),
                    GorselYolu = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oduller", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PuanBasvurulari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false),
                    AktiviteAdi = table.Column<string>(type: "TEXT", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", nullable: true),
                    Puan = table.Column<int>(type: "INTEGER", nullable: false),
                    FotografYolu = table.Column<string>(type: "TEXT", nullable: true),
                    Durum = table.Column<string>(type: "TEXT", nullable: false),
                    AdminNotu = table.Column<string>(type: "TEXT", nullable: true),
                    GondermeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuanBasvurulari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuanBasvurulari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Kullanicilar",
                columns: new[] { "Id", "Ad", "KullaniciAdi", "Rol", "Sifre" },
                values: new object[,]
                {
                    { 1, "Yönetici", "admin", "Admin", "$2a$11$CaTcykDyAFkpe9MX6Jobw.WQCfraHLCKUJrBpxex1oP6e2mPsOWYq" },
                    { 2, "Ali Yılmaz", "ali", "Ogrenci", "$2a$11$zZKeGD1Mk93HKqjVRMv13uXmoBL1Bw531UNgetUjNziZ8yz8K2UBe" }
                });

            migrationBuilder.InsertData(
                table: "Oduller",
                columns: new[] { "Id", "Aciklama", "Ad", "GerekliPuan", "GorselYolu" },
                values: new object[,]
                {
                    { 1, "Renkli kalem seti", "Kalem Seti", 50, null },
                    { 2, "Eğitici hikaye kitabı", "Kitap", 100, null },
                    { 3, "Deney ve bilim seti", "Bilim Seti", 250, null },
                    { 4, "Eğitim tableti", "Tablet", 500, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuanBasvurulari_KullaniciId",
                table: "PuanBasvurulari",
                column: "KullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Oduller");

            migrationBuilder.DropTable(
                name: "PuanBasvurulari");

            migrationBuilder.DropTable(
                name: "Kullanicilar");
        }
    }
}

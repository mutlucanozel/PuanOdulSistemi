using Microsoft.EntityFrameworkCore;
using PuanOdulSistemi.Models;

namespace PuanOdulSistemi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<PuanBasvurusu> PuanBasvurulari { get; set; }
        public DbSet<Odul> Oduller { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Admin kullanıcısı seed
            modelBuilder.Entity<Kullanici>().HasData(
                new Kullanici
                {
                    Id = 1,
                    Ad = "Yönetici",
                    KullaniciAdi = "admin",
                    Sifre = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Rol = "Admin"
                },
                new Kullanici
                {
                    Id = 2,
                    Ad = "Ali Yılmaz",
                    KullaniciAdi = "ali",
                    Sifre = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Rol = "Ogrenci"
                }
            );

            // Örnek ödüller
            modelBuilder.Entity<Odul>().HasData(
                new Odul { Id = 1, Ad = "Kalem Seti", Aciklama = "Renkli kalem seti", GerekliPuan = 50, GorselYolu = null },
                new Odul { Id = 2, Ad = "Kitap", Aciklama = "Eğitici hikaye kitabı", GerekliPuan = 100, GorselYolu = null },
                new Odul { Id = 3, Ad = "Bilim Seti", Aciklama = "Deney ve bilim seti", GerekliPuan = 250, GorselYolu = null },
                new Odul { Id = 4, Ad = "Tablet", Aciklama = "Eğitim tableti", GerekliPuan = 500, GorselYolu = null }
            );
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuanOdulSistemi.Data;
using PuanOdulSistemi.Models;

namespace PuanOdulSistemi.Controllers
{
    public class AdminController : Controller
    {
        private static readonly HashSet<string> IzinliGorselUzantilari = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private bool AdminMi()
        {
            return HttpContext.Session.GetString("Rol") == "Admin";
        }

        private void AdminViewBagHazirla()
        {
            ViewBag.Ad = HttpContext.Session.GetString("Ad");
            ViewBag.BekleyenSayi = _db.PuanBasvurulari.Count(p => p.Durum == "Bekliyor");
        }

        public IActionResult Index()
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            AdminViewBagHazirla();
            ViewBag.OgrenciSayisi = _db.Kullanicilar.Count(k => k.Rol == "Ogrenci");
            ViewBag.ToplamBasvuru = _db.PuanBasvurulari.Count();
            ViewBag.OdulSayisi = _db.Oduller.Count();
            return View();
        }

        public IActionResult OnayBekleyenler()
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            AdminViewBagHazirla();
            var bekleyenler = _db.PuanBasvurulari
                .Include(p => p.Kullanici)
                .Where(p => p.Durum == "Bekliyor")
                .OrderByDescending(p => p.GondermeTarihi)
                .ToList();

            return View(bekleyenler);
        }

        [HttpPost]
        public IActionResult Onayla(int id, string? adminNotu)
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            var basvuru = _db.PuanBasvurulari.Find(id);
            if (basvuru != null)
            {
                basvuru.Durum = "Onaylandi";
                basvuru.AdminNotu = adminNotu;
                _db.SaveChanges();
            }

            TempData["Basari"] = "Puan basariyla onaylandi.";
            return RedirectToAction("OnayBekleyenler");
        }

        [HttpPost]
        public IActionResult Reddet(int id, string? adminNotu)
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            var basvuru = _db.PuanBasvurulari.Find(id);
            if (basvuru != null)
            {
                basvuru.Durum = "Reddedildi";
                basvuru.AdminNotu = adminNotu;
                _db.SaveChanges();
            }

            TempData["Hata"] = "Puan reddedildi.";
            return RedirectToAction("OnayBekleyenler");
        }

        public IActionResult Ogrenciler()
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            AdminViewBagHazirla();
            var ogrenciler = _db.Kullanicilar
                .Where(k => k.Rol == "Ogrenci")
                .Include(k => k.PuanBasvurulari)
                .ToList();

            ViewBag.Oduller = _db.Oduller.ToList();
            ViewBag.Okullar = OkulBilgisi.Okullar;
            return View(ogrenciler);
        }

        [HttpGet]
        public IActionResult OgrenciEkle()
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");
            AdminViewBagHazirla();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OgrenciEkle(string ad, string kullaniciAdi, string sifre, IFormFile? profilFotograf)
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            if (_db.Kullanicilar.Any(k => k.KullaniciAdi == kullaniciAdi))
            {
                ViewBag.Hata = "Bu kullanici adi zaten kullaniliyor.";
                AdminViewBagHazirla();
                return View();
            }

            if (profilFotograf != null && !ProfilFotografGecerli(profilFotograf))
            {
                ViewBag.Hata = "Profil fotografi icin sadece JPG, PNG veya WEBP kullanabilirsiniz.";
                AdminViewBagHazirla();
                return View();
            }

            var profilFotografYolu = await ProfilFotografiKaydetAsync(profilFotograf);
            var yeniKullanici = new Kullanici
            {
                Ad = ad,
                KullaniciAdi = kullaniciAdi,
                Sifre = BCrypt.Net.BCrypt.HashPassword(sifre),
                Rol = "Ogrenci",
                ProfilFotografYolu = profilFotografYolu
            };

            _db.Kullanicilar.Add(yeniKullanici);
            _db.SaveChanges();

            TempData["Basari"] = $"{ad} adli ogrenci basariyla eklendi.";
            return RedirectToAction("Ogrenciler");
        }

        public IActionResult OdulleriYonet()
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            AdminViewBagHazirla();
            var oduller = _db.Oduller.OrderBy(o => o.GerekliPuan).ToList();
            return View(oduller);
        }

        [HttpPost]
        public async Task<IActionResult> OdulEkle(string ad, string? aciklama, int gerekliPuan, IFormFile? gorsel)
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            string? gorselYolu = null;
            if (gorsel != null && gorsel.Length > 0)
            {
                var uzanti = Path.GetExtension(gorsel.FileName);
                var dosyaAdi = $"odul_{Guid.NewGuid():N}{uzanti}";
                var klasor = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(klasor);
                var yuklemeYolu = Path.Combine(klasor, dosyaAdi);
                await using var stream = new FileStream(yuklemeYolu, FileMode.Create);
                await gorsel.CopyToAsync(stream);
                gorselYolu = $"/uploads/{dosyaAdi}";
            }

            var odul = new Odul
            {
                Ad = ad,
                Aciklama = aciklama,
                GerekliPuan = gerekliPuan,
                GorselYolu = gorselYolu
            };

            _db.Oduller.Add(odul);
            await _db.SaveChangesAsync();

            TempData["Basari"] = "Odul basariyla eklendi.";
            return RedirectToAction("OdulleriYonet");
        }

        [HttpPost]
        public IActionResult OdulSil(int id)
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            var odul = _db.Oduller.Find(id);
            if (odul != null)
            {
                _db.Oduller.Remove(odul);
                _db.SaveChanges();
            }

            TempData["Basari"] = "Odul silindi.";
            return RedirectToAction("OdulleriYonet");
        }

        private static bool ProfilFotografGecerli(IFormFile dosya)
        {
            var uzanti = Path.GetExtension(dosya.FileName);
            return !string.IsNullOrWhiteSpace(uzanti) && IzinliGorselUzantilari.Contains(uzanti);
        }

        private async Task<string?> ProfilFotografiKaydetAsync(IFormFile? dosya)
        {
            if (dosya == null || dosya.Length == 0) return null;

            var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
            var dosyaAdi = $"profil_{Guid.NewGuid():N}{uzanti}";
            var klasor = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(klasor);

            var tamYol = Path.Combine(klasor, dosyaAdi);
            await using var stream = new FileStream(tamYol, FileMode.Create);
            await dosya.CopyToAsync(stream);

            return $"/uploads/profiles/{dosyaAdi}";
        }
    }
}

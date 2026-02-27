using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuanOdulSistemi.Data;
using PuanOdulSistemi.Models;

namespace PuanOdulSistemi.Controllers
{
    public class AdminController : Controller
    {
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

        public IActionResult Index()
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            ViewBag.Ad = HttpContext.Session.GetString("Ad");
            ViewBag.BekleyenSayi = _db.PuanBasvurulari.Count(p => p.Durum == "Bekliyor");
            ViewBag.OgrenciSayisi = _db.Kullanicilar.Count(k => k.Rol == "Ogrenci");
            ViewBag.ToplamBasvuru = _db.PuanBasvurulari.Count();
            ViewBag.OdulSayisi = _db.Oduller.Count();
            return View();
        }

        // --- ONAY BEKLEYENLER ---
        public IActionResult OnayBekleyenler()
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");
            ViewBag.Ad = HttpContext.Session.GetString("Ad");
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
            TempData["Basari"] = "Puan başarıyla onaylandı.";
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

        // --- ÖĞRENCİLER ---
        public IActionResult Ogrenciler()
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");
            ViewBag.Ad = HttpContext.Session.GetString("Ad");

            var ogrenciler = _db.Kullanicilar
                .Where(k => k.Rol == "Ogrenci")
                .Include(k => k.PuanBasvurulari)
                .ToList();

            var oduller = _db.Oduller.ToList();
            ViewBag.Oduller = oduller;
            ViewBag.Okullar = OkulBilgisi.Okullar;
            return View(ogrenciler);
        }

        // --- ÖĞRENCİ EKLE ---
        [HttpGet]
        public IActionResult OgrenciEkle()
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");
            ViewBag.Ad = HttpContext.Session.GetString("Ad");
            return View();
        }

        [HttpPost]
        public IActionResult OgrenciEkle(string ad, string kullaniciAdi, string sifre)
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");

            if (_db.Kullanicilar.Any(k => k.KullaniciAdi == kullaniciAdi))
            {
                ViewBag.Hata = "Bu kullanıcı adı zaten kullanılıyor!";
                ViewBag.Ad = HttpContext.Session.GetString("Ad");
                return View();
            }

            var yeniKullanici = new Kullanici
            {
                Ad = ad,
                KullaniciAdi = kullaniciAdi,
                Sifre = BCrypt.Net.BCrypt.HashPassword(sifre),
                Rol = "Ogrenci"
            };
            _db.Kullanicilar.Add(yeniKullanici);
            _db.SaveChanges();

            TempData["Basari"] = $"{ad} adlı öğrenci başarıyla eklendi.";
            return RedirectToAction("Ogrenciler");
        }

        // --- ÖDÜLLERİ YÖNET ---
        public IActionResult OdulleriYonet()
        {
            if (!AdminMi()) return RedirectToAction("Giris", "Hesap");
            ViewBag.Ad = HttpContext.Session.GetString("Ad");
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
                var dosyaAdi = $"odul_{Guid.NewGuid()}{uzanti}";
                var yuklemeYolu = Path.Combine(_env.WebRootPath, "uploads", dosyaAdi);
                using var stream = new FileStream(yuklemeYolu, FileMode.Create);
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

            TempData["Basari"] = "Ödül başarıyla eklendi.";
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
            TempData["Basari"] = "Ödül silindi.";
            return RedirectToAction("OdulleriYonet");
        }
    }
}

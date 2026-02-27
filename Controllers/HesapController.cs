using Microsoft.AspNetCore.Mvc;
using PuanOdulSistemi.Data;
using PuanOdulSistemi.Models;

namespace PuanOdulSistemi.Controllers
{
    public class HesapController : Controller
    {
        private readonly AppDbContext _db;

        public HesapController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Giris()
        {
            if (HttpContext.Session.GetString("KullaniciId") != null)
            {
                var rol = HttpContext.Session.GetString("Rol");
                return rol == "Admin" ? RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Ogrenci");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Giris(string kullaniciAdi, string sifre)
        {
            var kullanici = _db.Kullanicilar.FirstOrDefault(k => k.KullaniciAdi == kullaniciAdi);
            if (kullanici == null || !BCrypt.Net.BCrypt.Verify(sifre, kullanici.Sifre))
            {
                ViewBag.Hata = "Kullanıcı adı veya şifre hatalı!";
                return View();
            }

            HttpContext.Session.SetString("KullaniciId", kullanici.Id.ToString());
            HttpContext.Session.SetString("KullaniciAdi", kullanici.KullaniciAdi);
            HttpContext.Session.SetString("Ad", kullanici.Ad);
            HttpContext.Session.SetString("Rol", kullanici.Rol);

            return kullanici.Rol == "Admin"
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Ogrenci");
        }

        [HttpGet]
        public IActionResult KayitOl()
        {
            if (HttpContext.Session.GetString("KullaniciId") != null)
            {
                var rol = HttpContext.Session.GetString("Rol");
                return rol == "Admin" ? RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Ogrenci");
            }
            return View();
        }

        [HttpPost]
        public IActionResult KayitOl(string ad, string kullaniciAdi, string sifre, string sifreTekrar)
        {
            if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
            {
                ViewBag.Hata = "Tüm alanları doldurunuz.";
                return View();
            }

            if (sifre.Length < 6)
            {
                ViewBag.Hata = "Şifre en az 6 karakter olmalıdır.";
                return View();
            }

            if (sifre != sifreTekrar)
            {
                ViewBag.Hata = "Şifreler eşleşmiyor!";
                return View();
            }

            if (_db.Kullanicilar.Any(k => k.KullaniciAdi == kullaniciAdi))
            {
                ViewBag.Hata = "Bu kullanıcı adı zaten kullanılıyor!";
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

            TempData["Basari"] = "Kayıt başarıyla tamamlandı! Şimdi giriş yapabilirsiniz.";
            return RedirectToAction("Giris");
        }

        public IActionResult Cikis()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Giris");
        }
    }
}

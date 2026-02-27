using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using PuanOdulSistemi.Data;
using PuanOdulSistemi.Models;

namespace PuanOdulSistemi.Controllers
{
    public class HesapController : Controller
    {
        private const string BeniHatirlaCerezAdi = "LgsHazirlik.Remember";
        private static readonly TimeSpan BeniHatirlaSuresi = TimeSpan.FromDays(30);

        private static readonly HashSet<string> IzinliGorselUzantilari = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _beniHatirlaKoruyucu;

        public HesapController(AppDbContext db, IWebHostEnvironment env, IDataProtectionProvider dataProtectionProvider)
        {
            _db = db;
            _env = env;
            _beniHatirlaKoruyucu = dataProtectionProvider.CreateProtector("LgsHazirlik.Remember.v1");
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
        public IActionResult Giris(string kullaniciAdi, string sifre, bool beniHatirla = false)
        {
            var kullanici = _db.Kullanicilar.FirstOrDefault(k => k.KullaniciAdi == kullaniciAdi);
            if (kullanici == null || !BCrypt.Net.BCrypt.Verify(sifre, kullanici.Sifre))
            {
                ViewBag.Hata = "Kullanici adi veya sifre hatali.";
                return View();
            }

            SessionYaz(kullanici);
            if (beniHatirla)
            {
                BeniHatirlaCereziniYaz(kullanici.Id);
            }
            else
            {
                BeniHatirlaCereziniSil();
            }

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
        public async Task<IActionResult> KayitOl(string ad, string kullaniciAdi, string sifre, string sifreTekrar, IFormFile? profilFotograf)
        {
            if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
            {
                ViewBag.Hata = "Tum alanlari doldurunuz.";
                return View();
            }

            if (sifre.Length < 6)
            {
                ViewBag.Hata = "Sifre en az 6 karakter olmali.";
                return View();
            }

            if (sifre != sifreTekrar)
            {
                ViewBag.Hata = "Sifreler eslesmiyor.";
                return View();
            }

            if (_db.Kullanicilar.Any(k => k.KullaniciAdi == kullaniciAdi))
            {
                ViewBag.Hata = "Bu kullanici adi zaten kullaniliyor.";
                return View();
            }

            if (profilFotograf != null && !ProfilFotografGecerli(profilFotograf))
            {
                ViewBag.Hata = "Profil fotografi icin sadece JPG, PNG veya WEBP kullanabilirsiniz.";
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

            TempData["Basari"] = "Kayit basariyla tamamlandi. Simdi giris yapabilirsiniz.";
            return RedirectToAction("Giris");
        }

        [HttpGet]
        public IActionResult Profil()
        {
            var id = SessionKullaniciId();
            if (id == null) return RedirectToAction("Giris");

            var kullanici = _db.Kullanicilar.FirstOrDefault(k => k.Id == id.Value);
            if (kullanici == null) return RedirectToAction("Giris");

            ViewData["Active"] = "Profil";
            ViewBag.Ad = kullanici.Ad;
            if (kullanici.Rol == "Admin")
            {
                ViewBag.BekleyenSayi = _db.PuanBasvurulari.Count(p => p.Durum == "Bekliyor");
            }
            return View(kullanici);
        }

        [HttpPost]
        public async Task<IActionResult> Profil(IFormFile? profilFotograf)
        {
            var id = SessionKullaniciId();
            if (id == null) return RedirectToAction("Giris");

            var kullanici = _db.Kullanicilar.FirstOrDefault(k => k.Id == id.Value);
            if (kullanici == null) return RedirectToAction("Giris");

            if (profilFotograf == null || profilFotograf.Length == 0)
            {
                TempData["Hata"] = "Lutfen bir fotograf secin.";
                return RedirectToAction("Profil");
            }

            if (!ProfilFotografGecerli(profilFotograf))
            {
                TempData["Hata"] = "Profil fotografi icin sadece JPG, PNG veya WEBP kullanabilirsiniz.";
                return RedirectToAction("Profil");
            }

            var profilFotografYolu = await ProfilFotografiKaydetAsync(profilFotograf);
            kullanici.ProfilFotografYolu = profilFotografYolu;
            _db.SaveChanges();

            HttpContext.Session.SetString("ProfilFotografYolu", profilFotografYolu ?? string.Empty);
            TempData["Basari"] = "Profil fotografi guncellendi.";
            return RedirectToAction("Profil");
        }

        public IActionResult Cikis()
        {
            HttpContext.Session.Clear();
            BeniHatirlaCereziniSil();
            return RedirectToAction("Giris");
        }

        private int? SessionKullaniciId()
        {
            var idStr = HttpContext.Session.GetString("KullaniciId");
            if (string.IsNullOrWhiteSpace(idStr)) return null;
            return int.TryParse(idStr, out var id) ? id : null;
        }

        private void SessionYaz(Kullanici kullanici)
        {
            HttpContext.Session.SetString("KullaniciId", kullanici.Id.ToString());
            HttpContext.Session.SetString("KullaniciAdi", kullanici.KullaniciAdi);
            HttpContext.Session.SetString("Ad", kullanici.Ad);
            HttpContext.Session.SetString("Rol", kullanici.Rol);
            HttpContext.Session.SetString("ProfilFotografYolu", kullanici.ProfilFotografYolu ?? string.Empty);
        }

        private void BeniHatirlaCereziniYaz(int kullaniciId)
        {
            var gecerlilikBitis = DateTimeOffset.UtcNow.Add(BeniHatirlaSuresi);
            var hamVeri = $"{kullaniciId}|{gecerlilikBitis.ToUnixTimeSeconds()}";
            var korumaliToken = _beniHatirlaKoruyucu.Protect(hamVeri);

            Response.Cookies.Append(BeniHatirlaCerezAdi, korumaliToken, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = gecerlilikBitis,
                Path = "/"
            });
        }

        private void BeniHatirlaCereziniSil()
        {
            Response.Cookies.Delete(BeniHatirlaCerezAdi, new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });
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

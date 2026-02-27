using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuanOdulSistemi.Data;
using PuanOdulSistemi.Models;

namespace PuanOdulSistemi.Controllers
{
    public class OgrenciController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public OgrenciController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private int? GetKullaniciId()
        {
            var idStr = HttpContext.Session.GetString("KullaniciId");
            if (string.IsNullOrEmpty(idStr) || HttpContext.Session.GetString("Rol") != "Ogrenci")
                return null;
            return int.Parse(idStr);
        }

        public IActionResult Index()
        {
            var id = GetKullaniciId();
            if (id == null) return RedirectToAction("Giris", "Hesap");

            var basvurular = _db.PuanBasvurulari.Where(p => p.KullaniciId == id).ToList();
            var oduller = _db.Oduller.OrderBy(o => o.GerekliPuan).ToList();
            var toplamPuan = basvurular.Where(b => b.Durum == "Onaylandi").Sum(b => b.Puan);
            var bekleyenSayi = basvurular.Count(b => b.Durum == "Bekliyor");
            var onaylananSayi = basvurular.Count(b => b.Durum == "Onaylandi");
            var ortalama = onaylananSayi > 0 ? basvurular.Where(b => b.Durum == "Onaylandi").Average(b => b.Puan) : 0;
            var kazanilanOduller = oduller.Where(o => o.GerekliPuan <= toplamPuan).ToList();

            ViewBag.Ad = HttpContext.Session.GetString("Ad");
            ViewBag.ToplamPuan = toplamPuan;
            ViewBag.BekleyenSayi = bekleyenSayi;
            ViewBag.OnaylananSayi = onaylananSayi;
            ViewBag.Ortalama = ortalama;
            ViewBag.KazanilanOduller = kazanilanOduller;
            ViewBag.TumOduller = oduller;
            ViewBag.Okullar = OkulBilgisi.Okullar;

            return View(basvurular.OrderByDescending(b => b.GondermeTarihi).Take(5).ToList());
        }

        [HttpGet]
        public IActionResult PuanGir()
        {
            if (GetKullaniciId() == null) return RedirectToAction("Giris", "Hesap");
            ViewBag.Ad = HttpContext.Session.GetString("Ad");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PuanGir(string aktiviteAdi, string? aciklama, int puan, IFormFile? fotograf)
        {
            var id = GetKullaniciId();
            if (id == null) return RedirectToAction("Giris", "Hesap");

            string? fotografYolu = null;
            if (fotograf != null && fotograf.Length > 0)
            {
                var uzanti = Path.GetExtension(fotograf.FileName);
                var dosyaAdi = $"{Guid.NewGuid()}{uzanti}";
                var yuklemeYolu = Path.Combine(_env.WebRootPath, "uploads", dosyaAdi);
                using var stream = new FileStream(yuklemeYolu, FileMode.Create);
                await fotograf.CopyToAsync(stream);
                fotografYolu = $"/uploads/{dosyaAdi}";
            }

            var basvuru = new PuanBasvurusu
            {
                KullaniciId = id.Value,
                AktiviteAdi = aktiviteAdi,
                Aciklama = aciklama,
                Puan = puan,
                FotografYolu = fotografYolu,
                Durum = "Bekliyor",
                GondermeTarihi = DateTime.Now
            };

            _db.PuanBasvurulari.Add(basvuru);
            await _db.SaveChangesAsync();

            TempData["Basari"] = "Puanınız başarıyla gönderildi! Admin onayı bekleniyor.";
            return RedirectToAction("Puanlarim");
        }

        public IActionResult Puanlarim()
        {
            var id = GetKullaniciId();
            if (id == null) return RedirectToAction("Giris", "Hesap");

            ViewBag.Ad = HttpContext.Session.GetString("Ad");
            var basvurular = _db.PuanBasvurulari
                .Where(p => p.KullaniciId == id)
                .OrderByDescending(p => p.GondermeTarihi)
                .ToList();

            return View(basvurular);
        }

        public IActionResult Oduller()
        {
            var id = GetKullaniciId();
            if (id == null) return RedirectToAction("Giris", "Hesap");

            var onaylananlar = _db.PuanBasvurulari
                .Where(p => p.KullaniciId == id && p.Durum == "Onaylandi")
                .ToList();

            var toplamPuan = onaylananlar.Sum(p => p.Puan);
            var ortalama = onaylananlar.Count > 0 ? onaylananlar.Average(p => p.Puan) : 0;

            var oduller = _db.Oduller.OrderBy(o => o.GerekliPuan).ToList();

            ViewBag.Ad = HttpContext.Session.GetString("Ad");
            ViewBag.ToplamPuan = toplamPuan;
            ViewBag.Ortalama = ortalama;
            return View(oduller);
        }

        public IActionResult OkulTahmini()
        {
            var id = GetKullaniciId();
            if (id == null) return RedirectToAction("Giris", "Hesap");

            var onaylananlar = _db.PuanBasvurulari
                .Where(p => p.KullaniciId == id && p.Durum == "Onaylandi")
                .ToList();

            double ortalama = onaylananlar.Count > 0 ? onaylananlar.Average(p => p.Puan) : 0;

            ViewBag.Ad = HttpContext.Session.GetString("Ad");
            ViewBag.Ortalama = ortalama;
            ViewBag.Okullar = OkulBilgisi.Okullar;
            return View();
        }
    }
}

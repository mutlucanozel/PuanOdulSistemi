using System.ComponentModel.DataAnnotations;

namespace PuanOdulSistemi.Models
{
    public class PuanBasvurusu
    {
        public int Id { get; set; }

        public int KullaniciId { get; set; }
        public Kullanici? Kullanici { get; set; }

        [Required]
        [Display(Name = "Aktivite Adı")]
        public string AktiviteAdi { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        [Required]
        [Range(1, 10000)]
        [Display(Name = "İstenen Puan")]
        public int Puan { get; set; }

        [Display(Name = "Kanıt Fotoğrafı")]
        public string? FotografYolu { get; set; }

        // "Bekliyor", "Onaylandi", "Reddedildi"
        public string Durum { get; set; } = "Bekliyor";

        [Display(Name = "Admin Notu")]
        public string? AdminNotu { get; set; }

        public DateTime GondermeTarihi { get; set; } = DateTime.Now;
    }
}

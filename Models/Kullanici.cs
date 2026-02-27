using System.ComponentModel.DataAnnotations;

namespace PuanOdulSistemi.Models
{
    public class Kullanici
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Ad Soyad")]
        public string Ad { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required]
        public string Sifre { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = "Ogrenci"; // "Ogrenci" veya "Admin"

        public string? ProfilFotografYolu { get; set; }

        public ICollection<PuanBasvurusu> PuanBasvurulari { get; set; } = new List<PuanBasvurusu>();
    }
}

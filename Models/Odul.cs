using System.ComponentModel.DataAnnotations;

namespace PuanOdulSistemi.Models
{
    public class Odul
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Ödül Adı")]
        public string Ad { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        [Required]
        [Range(1, 100000)]
        [Display(Name = "Gerekli Puan")]
        public int GerekliPuan { get; set; }

        [Display(Name = "Görsel")]
        public string? GorselYolu { get; set; }
    }
}

namespace PuanOdulSistemi.Models
{
    public class OkulBilgisi
    {
        public string Ad { get; set; } = string.Empty;
        public double TabanPuan { get; set; }

        public static List<OkulBilgisi> Okullar => new List<OkulBilgisi>
        {
            new OkulBilgisi { Ad = "Eskişehir Fatih Fen Lisesi", TabanPuan = 476.26 },
            new OkulBilgisi { Ad = "Eskişehir Anadolu Lisesi", TabanPuan = 464.91 },
            new OkulBilgisi { Ad = "Borsa İstanbul Fen Lisesi", TabanPuan = 453.46 },
            new OkulBilgisi { Ad = "Şehit Mehmet Şengül Fen Lisesi", TabanPuan = 451.07 },
            new OkulBilgisi { Ad = "Atatürk Lisesi", TabanPuan = 440.84 },
            new OkulBilgisi { Ad = "Sabiha Gökçen MTAL – Uçak Bakım Alanı (ATP)", TabanPuan = 420.25 },
            new OkulBilgisi { Ad = "Eskişehir Eti Sosyal Bilimler Lisesi", TabanPuan = 417.09 },
        };
    }
}

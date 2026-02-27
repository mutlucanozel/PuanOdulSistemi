using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using PuanOdulSistemi.Data;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// DB oluştur ve migrate et
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    ProfiliFotografiKolonunuGarantiEt(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.Use(async (context, next) =>
{
    const string beniHatirlaCerezAdi = "LgsHazirlik.Remember";
    const string cerezKorumaAmaci = "LgsHazirlik.Remember.v1";

    var oturumKullaniciId = context.Session.GetString("KullaniciId");
    if (string.IsNullOrWhiteSpace(oturumKullaniciId)
        && context.Request.Cookies.TryGetValue(beniHatirlaCerezAdi, out var token)
        && !string.IsNullOrWhiteSpace(token))
    {
        var protector = context.RequestServices
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(cerezKorumaAmaci);

        try
        {
            var acikVeri = protector.Unprotect(token);
            var parcalar = acikVeri.Split('|', StringSplitOptions.RemoveEmptyEntries);

            if (parcalar.Length == 2
                && int.TryParse(parcalar[0], out var kullaniciId)
                && long.TryParse(parcalar[1], out var gecerlilikUnix))
            {
                var gecerlilikBitis = DateTimeOffset.FromUnixTimeSeconds(gecerlilikUnix);
                if (gecerlilikBitis > DateTimeOffset.UtcNow)
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var kullanici = await db.Kullanicilar
                        .AsNoTracking()
                        .FirstOrDefaultAsync(k => k.Id == kullaniciId);

                    if (kullanici is not null)
                    {
                        context.Session.SetString("KullaniciId", kullanici.Id.ToString());
                        context.Session.SetString("KullaniciAdi", kullanici.KullaniciAdi);
                        context.Session.SetString("Ad", kullanici.Ad);
                        context.Session.SetString("Rol", kullanici.Rol);
                        context.Session.SetString("ProfilFotografYolu", kullanici.ProfilFotografYolu ?? string.Empty);

                        var yeniBitis = DateTimeOffset.UtcNow.AddDays(30);
                        var yeniAcikVeri = $"{kullanici.Id}|{yeniBitis.ToUnixTimeSeconds()}";
                        var yeniToken = protector.Protect(yeniAcikVeri);

                        context.Response.Cookies.Append(beniHatirlaCerezAdi, yeniToken, new CookieOptions
                        {
                            HttpOnly = true,
                            IsEssential = true,
                            SameSite = SameSiteMode.Lax,
                            Secure = context.Request.IsHttps,
                            Expires = yeniBitis,
                            Path = "/"
                        });
                    }
                    else
                    {
                        BeniHatirlaCereziniSil(context, beniHatirlaCerezAdi);
                    }
                }
                else
                {
                    BeniHatirlaCereziniSil(context, beniHatirlaCerezAdi);
                }
            }
            else
            {
                BeniHatirlaCereziniSil(context, beniHatirlaCerezAdi);
            }
        }
        catch
        {
            BeniHatirlaCereziniSil(context, beniHatirlaCerezAdi);
        }
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Hesap}/{action=Giris}/{id?}");

app.Run();

static void ProfiliFotografiKolonunuGarantiEt(AppDbContext db)
{
    var connection = db.Database.GetDbConnection();
    var connectionInitiallyOpen = connection.State == ConnectionState.Open;

    if (!connectionInitiallyOpen)
    {
        connection.Open();
    }

    try
    {
        var kolonVar = false;
        using (var kontrolKomutu = connection.CreateCommand())
        {
            kontrolKomutu.CommandText = "PRAGMA table_info('Kullanicilar');";
            using var reader = kontrolKomutu.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader["name"]?.ToString(), "ProfilFotografYolu", StringComparison.OrdinalIgnoreCase))
                {
                    kolonVar = true;
                    break;
                }
            }
        }

        if (!kolonVar)
        {
            using var alterKomutu = connection.CreateCommand();
            alterKomutu.CommandText = "ALTER TABLE Kullanicilar ADD COLUMN ProfilFotografYolu TEXT NULL;";
            alterKomutu.ExecuteNonQuery();
        }
    }
    finally
    {
        if (!connectionInitiallyOpen)
        {
            connection.Close();
        }
    }
}

static void BeniHatirlaCereziniSil(HttpContext context, string cerezAdi)
{
    context.Response.Cookies.Delete(cerezAdi, new CookieOptions
    {
        Path = "/",
        SameSite = SameSiteMode.Lax,
        Secure = context.Request.IsHttps
    });
}

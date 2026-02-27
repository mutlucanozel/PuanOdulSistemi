using Microsoft.EntityFrameworkCore;
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

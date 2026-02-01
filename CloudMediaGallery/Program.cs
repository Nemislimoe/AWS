using CloudMediaGallery.Data;
using CloudMediaGallery.Models;
using CloudMediaGallery.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Конфігурація DB (SQLite за замовчуванням)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (реєстрація/логін)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Спрощені правила пароля для навчального проєкту
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// MVC
builder.Services.AddControllersWithViews();

// Налаштування StorageOptions з appsettings
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

// Реєстрація BlobService як singleton
builder.Services.AddSingleton<IBlobService, BlobService>();

var app = builder.Build();

// Автоматичне створення БД та застосування міграцій при запуску (для зручності)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Media}/{action=Index}/{id?}");

app.Run();

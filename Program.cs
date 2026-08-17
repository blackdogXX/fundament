using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ConstructionFinance.Data;
using ConstructionFinance.Models;
using ConstructionFinance.Services;
using ConstructionFinance.Components;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=data/ConstructionFinance.db"));

var keysPath = builder.Configuration["DbPath"] is string dbp
    ? Path.Combine(Path.GetDirectoryName(dbp) ?? "data", "keys")
    : "data/keys";
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("Fundament");

// --- Почта ---
var emailSettings = new EmailSettings
{
    Host = builder.Configuration["SMTP_HOST"] ?? "",
    Port = int.TryParse(builder.Configuration["SMTP_PORT"], out var smtpPort) ? smtpPort : 587,
    User = builder.Configuration["SMTP_USER"] ?? "",
    Password = builder.Configuration["SMTP_PASS"] ?? "",
    From = builder.Configuration["SMTP_FROM"] ?? "",
    FromName = builder.Configuration["SMTP_FROM_NAME"] ?? "Fundament",
    Security = builder.Configuration["SMTP_SECURITY"] ?? "auto",
    AllowInvalidCert = string.Equals(builder.Configuration["SMTP_ALLOW_INVALID_CERT"], "true", StringComparison.OrdinalIgnoreCase)
};
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<EmailService>();

var appOptions = new AppOptions
{
    BaseUrl = builder.Configuration["APP_BASE_URL"] ?? "",
    // Подтверждение обязательно только если почта реально настроена — иначе
    // пользователи не смогли бы войти вообще.
    RequireConfirmedEmail = !string.Equals(builder.Configuration["REQUIRE_CONFIRMED_EMAIL"], "false", StringComparison.OrdinalIgnoreCase)
                            && emailSettings.Enabled
};
builder.Services.AddSingleton(appOptions);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = appOptions.RequireConfirmedEmail;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Срок жизни ссылок подтверждения и сброса пароля
builder.Services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(24));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<FinService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<AdminService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

await DbInit.InitAsync(app.Services);
await DbInit.EnsureAdminAsync(app.Services, builder.Configuration["ADMIN_EMAIL"]);

app.Logger.LogInformation(
    "Почта: {State}. Подтверждение адреса при регистрации: {Confirm}.",
    emailSettings.Enabled ? $"включена, отправитель {emailSettings.From} через {emailSettings.Host}:{emailSettings.Port}" : "выключена (SMTP_HOST/SMTP_FROM не заданы)",
    appOptions.RequireConfirmedEmail ? "обязательно" : "не требуется");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/api/backup/download", async (HttpContext http, BackupService backup) =>
{
    var uid = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(uid)) return Results.Unauthorized();

    var bytes = await backup.ExportBytes(uid);
    var filename = $"fundament-backup-{DateTime.Now:yyyy-MM-dd-HHmm}.json";
    return Results.File(bytes, "application/json", filename);
}).RequireAuthorization();

app.Run();

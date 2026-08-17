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

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

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

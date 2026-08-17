using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using ConstructionFinance.Data;
using ConstructionFinance.Models;

namespace ConstructionFinance.Services;

public class BackupFile
{
    public string Format { get; set; } = "fundament-backup";
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<BackupSite> Sites { get; set; } = new();
    public List<BackupCategory> Categories { get; set; } = new();
    public List<BackupIncome> Incomes { get; set; } = new();
    public List<BackupExpense> Expenses { get; set; } = new();
    public List<BackupSetting> Settings { get; set; } = new();
}

public class BackupSite
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public DateTime StartDate { get; set; }
    public SiteStatus Status { get; set; }
    public bool IsDefault { get; set; }
}

public class BackupCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<BackupSubcategory> Subcategories { get; set; } = new();
}

public class BackupSubcategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class BackupIncome
{
    public double Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public int? SiteId { get; set; }
}

public class BackupExpense
{
    public double Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public int? SiteId { get; set; }
    public int? CategoryId { get; set; }
    public int? SubcategoryId { get; set; }
}

public class BackupSetting
{
    public string Key { get; set; } = "";
    public string? Value { get; set; }
}

public class BackupService
{
    private readonly AppDbContext _ctx;
    public BackupService(AppDbContext ctx) { _ctx = ctx; }

    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<BackupFile> Export(string uid)
    {
        var file = new BackupFile();

        file.Sites = await _ctx.Sites
            .Where(s => s.UserId == uid)
            .OrderBy(s => s.Id)
            .Select(s => new BackupSite
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                StartDate = s.StartDate,
                Status = s.Status,
                IsDefault = s.IsDefault
            })
            .ToListAsync();

        file.Categories = await _ctx.Categories
            .Where(c => c.UserId == uid)
            .OrderBy(c => c.Id)
            .Select(c => new BackupCategory
            {
                Id = c.Id,
                Name = c.Name,
                Subcategories = c.Subcategories
                    .OrderBy(s => s.Id)
                    .Select(s => new BackupSubcategory { Id = s.Id, Name = s.Name })
                    .ToList()
            })
            .ToListAsync();

        file.Incomes = await _ctx.Incomes
            .Where(i => i.UserId == uid)
            .OrderBy(i => i.Id)
            .Select(i => new BackupIncome
            {
                Amount = i.Amount,
                Description = i.Description,
                Date = i.Date,
                SiteId = i.SiteId
            })
            .ToListAsync();

        file.Expenses = await _ctx.Expenses
            .Where(e => e.UserId == uid)
            .OrderBy(e => e.Id)
            .Select(e => new BackupExpense
            {
                Amount = e.Amount,
                Description = e.Description,
                Date = e.Date,
                SiteId = e.SiteId,
                CategoryId = e.CategoryId,
                SubcategoryId = e.SubcategoryId
            })
            .ToListAsync();

        file.Settings = await _ctx.UserSettings
            .Where(s => s.UserId == uid)
            .OrderBy(s => s.Id)
            .Select(s => new BackupSetting { Key = s.Key, Value = s.Value })
            .ToListAsync();

        return file;
    }

    public async Task<byte[]> ExportBytes(string uid)
        => JsonSerializer.SerializeToUtf8Bytes(await Export(uid), JsonOpts);
}

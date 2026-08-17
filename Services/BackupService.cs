using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using ConstructionFinance.Data;
using ConstructionFinance.Models;

namespace ConstructionFinance.Services;

public enum ImportMode { Merge, Replace }

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

public class ImportResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public int Sites { get; set; }
    public int Categories { get; set; }
    public int Subcategories { get; set; }
    public int Incomes { get; set; }
    public int Expenses { get; set; }
    public int SkippedIncomes { get; set; }
    public int SkippedExpenses { get; set; }
    public int Deleted { get; set; }
}

public class BackupService
{
    private readonly AppDbContext _ctx;
    public BackupService(AppDbContext ctx) { _ctx = ctx; }

    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
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

    public async Task<ImportResult> Import(string uid, Stream json, ImportMode mode)
    {
        BackupFile? file;
        try
        {
            file = await JsonSerializer.DeserializeAsync<BackupFile>(json, JsonOpts);
        }
        catch (JsonException ex)
        {
            return new ImportResult { Error = "Файл не читается как JSON: " + ex.Message };
        }

        if (file == null)
            return new ImportResult { Error = "Файл пустой" };
        if (file.Format != "fundament-backup")
            return new ImportResult { Error = "Это не файл резервной копии Fundament" };
        if (file.Version > 1)
            return new ImportResult { Error = $"Версия файла {file.Version} новее, чем понимает приложение" };

        var r = new ImportResult();
        using var tx = await _ctx.Database.BeginTransactionAsync();

        try
        {
            if (mode == ImportMode.Replace)
            {
                var oldExp = await _ctx.Expenses.Where(e => e.UserId == uid).ToListAsync();
                var oldInc = await _ctx.Incomes.Where(i => i.UserId == uid).ToListAsync();
                var oldSites = await _ctx.Sites.Where(s => s.UserId == uid && s.IsDefault == false).ToListAsync();
                var oldCats = await _ctx.Categories.Where(c => c.UserId == uid).ToListAsync();
                r.Deleted = oldExp.Count + oldInc.Count + oldSites.Count + oldCats.Count;
                _ctx.Expenses.RemoveRange(oldExp);
                _ctx.Incomes.RemoveRange(oldInc);
                _ctx.Sites.RemoveRange(oldSites);
                _ctx.Categories.RemoveRange(oldCats);
                await _ctx.SaveChangesAsync();
            }

            var siteMap = new Dictionary<int, int>();
            var sites = await _ctx.Sites.Where(s => s.UserId == uid).ToListAsync();
            var defSite = sites.FirstOrDefault(s => s.IsDefault);

            foreach (var s in file.Sites)
            {
                if (s.IsDefault && defSite != null)
                {
                    if (mode == ImportMode.Replace && defSite.Name != s.Name) defSite.Name = s.Name;
                    siteMap[s.Id] = defSite.Id;
                    continue;
                }

                var match = sites.FirstOrDefault(x => x.IsDefault == false && x.Name == s.Name);
                if (match != null)
                {
                    siteMap[s.Id] = match.Id;
                    continue;
                }

                var ns = new Site
                {
                    Name = s.Name,
                    Address = s.Address,
                    StartDate = s.StartDate,
                    Status = s.Status,
                    IsDefault = s.IsDefault && defSite == null,
                    UserId = uid
                };
                _ctx.Sites.Add(ns);
                await _ctx.SaveChangesAsync();
                sites.Add(ns);
                if (ns.IsDefault) defSite = ns;
                siteMap[s.Id] = ns.Id;
                r.Sites++;
            }

            var catMap = new Dictionary<int, int>();
            var subMap = new Dictionary<int, int>();
            var cats = await _ctx.Categories.Where(c => c.UserId == uid).Include(c => c.Subcategories).ToListAsync();

            foreach (var c in file.Categories)
            {
                var cat = cats.FirstOrDefault(x => x.Name == c.Name);
                if (cat == null)
                {
                    cat = new Category { Name = c.Name, UserId = uid };
                    _ctx.Categories.Add(cat);
                    await _ctx.SaveChangesAsync();
                    cats.Add(cat);
                    r.Categories++;
                }
                catMap[c.Id] = cat.Id;

                foreach (var s in c.Subcategories)
                {
                    var sub = cat.Subcategories.FirstOrDefault(x => x.Name == s.Name);
                    if (sub == null)
                    {
                        sub = new Subcategory { Name = s.Name, CategoryId = cat.Id };
                        _ctx.Subcategories.Add(sub);
                        await _ctx.SaveChangesAsync();
                        cat.Subcategories.Add(sub);
                        r.Subcategories++;
                    }
                    subMap[s.Id] = sub.Id;
                }
            }

            foreach (var i in file.Incomes)
            {
                int? siteId = i.SiteId.HasValue && siteMap.TryGetValue(i.SiteId.Value, out var sid) ? sid : null;

                if (mode == ImportMode.Merge)
                {
                    var dup = await _ctx.Incomes.AnyAsync(x =>
                        x.UserId == uid && x.Amount == i.Amount && x.Date == i.Date &&
                        x.SiteId == siteId && x.Description == i.Description);
                    if (dup) { r.SkippedIncomes++; continue; }
                }

                _ctx.Incomes.Add(new Income
                {
                    Amount = i.Amount,
                    Description = i.Description,
                    Date = i.Date,
                    SiteId = siteId,
                    UserId = uid
                });
                r.Incomes++;
                await _ctx.SaveChangesAsync();
            }

            foreach (var e in file.Expenses)
            {
                int? siteId = e.SiteId.HasValue && siteMap.TryGetValue(e.SiteId.Value, out var sid) ? sid : null;
                int? catId = e.CategoryId.HasValue && catMap.TryGetValue(e.CategoryId.Value, out var cid) ? cid : null;
                int? subId = e.SubcategoryId.HasValue && subMap.TryGetValue(e.SubcategoryId.Value, out var bid) ? bid : null;

                if (mode == ImportMode.Merge)
                {
                    var dup = await _ctx.Expenses.AnyAsync(x =>
                        x.UserId == uid && x.Amount == e.Amount && x.Date == e.Date &&
                        x.SiteId == siteId && x.Description == e.Description);
                    if (dup) { r.SkippedExpenses++; continue; }
                }

                _ctx.Expenses.Add(new Expense
                {
                    Amount = e.Amount,
                    Description = e.Description,
                    Date = e.Date,
                    SiteId = siteId,
                    CategoryId = catId,
                    SubcategoryId = subId,
                    UserId = uid
                });
                r.Expenses++;
                await _ctx.SaveChangesAsync();
            }

            foreach (var s in file.Settings)
            {
                var us = await _ctx.UserSettings.FirstOrDefaultAsync(x => x.UserId == uid && x.Key == s.Key);
                if (us != null) us.Value = s.Value;
                else _ctx.UserSettings.Add(new UserSetting { UserId = uid, Key = s.Key, Value = s.Value });
            }

            await _ctx.SaveChangesAsync();
            await tx.CommitAsync();
            r.Ok = true;
            return r;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return new ImportResult { Error = "Импорт прерван, изменения откачены: " + ex.Message };
        }
    }
}

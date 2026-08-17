using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Con
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
                var oldSites = await _ctx.Sites.Where(s => s.UserId == uid && !s.IsDefault).ToListAsync();
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

                var match = sites.FirstOrDefault(x => !x.IsDefault && x.Name == s.Name);
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

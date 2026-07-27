using Microsoft.EntityFrameworkCore;
using ConstructionFinance.Data;
using ConstructionFinance.Models;
namespace ConstructionFinance.Services;
public class FinService
{
    private readonly AppDbContext _ctx;
    private readonly IHttpContextAccessor _http;
    public FinService(AppDbContext ctx, IHttpContextAccessor http) { _ctx = ctx; _http = http; }
    private string Uid => _http.HttpContext!.User.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;

    public async Task<double> Balance() => (await _ctx.Incomes.Where(i => i.UserId == Uid).SumAsync(i => i.Amount)) - (await _ctx.Expenses.Where(e => e.UserId == Uid).SumAsync(e => e.Amount));

    public async Task<List<Income>> GetIncomes() => await _ctx.Incomes.Where(i => i.UserId == Uid).Include(i => i.Site).OrderByDescending(i => i.Date).ToListAsync();
    public async Task AddIncome(Income i) { i.UserId = Uid; _ctx.Incomes.Add(i); await _ctx.SaveChangesAsync(); }
    public async Task DelIncome(int id) { var i = await _ctx.Incomes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid); if (i != null) { _ctx.Incomes.Remove(i); await _ctx.SaveChangesAsync(); } }
    public async Task UpdateIncome(Income income) { var e = await _ctx.Incomes.FirstOrDefaultAsync(i => i.Id == income.Id && i.UserId == Uid); if (e != null) { e.Amount = income.Amount; e.Description = income.Description; e.Date = income.Date; e.SiteId = income.SiteId; await _ctx.SaveChangesAsync(); } }

    public async Task<List<Site>> GetSites() => await _ctx.Sites.Where(s => s.UserId == Uid && !s.IsDefault).OrderBy(s => s.Status).ThenBy(s => s.Name).ToListAsync();
    public async Task<List<Site>> GetAllSites() => await _ctx.Sites.Where(s => s.UserId == Uid).OrderBy(s => s.Name).ToListAsync();
    public async Task<double> SiteExp(int id) => await _ctx.Expenses.Where(e => e.UserId == Uid && e.SiteId == id).SumAsync(e => e.Amount);
    public async Task AddSite(Site s) { s.UserId = Uid; _ctx.Sites.Add(s); await _ctx.SaveChangesAsync(); }
    public async Task UpdateSite(Site s) { var e = await _ctx.Sites.FirstOrDefaultAsync(x => x.Id == s.Id && x.UserId == Uid); if (e != null) { e.Name = s.Name; e.Address = s.Address; e.Status = s.Status; await _ctx.SaveChangesAsync(); } }
    public async Task UpdSiteStatus(int id, SiteStatus st) { var s = await _ctx.Sites.FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid); if (s != null) { s.Status = st; await _ctx.SaveChangesAsync(); } }
    public async Task DeleteSite(int id) { var s = await _ctx.Sites.FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid && !x.IsDefault); if (s != null) { _ctx.Sites.Remove(s); await _ctx.SaveChangesAsync(); } }
    public async Task<bool> SiteHasExpenses(int id) => await _ctx.Expenses.AnyAsync(e => e.SiteId == id && e.UserId == Uid);
    public async Task<bool> SiteHasIncomes(int id) => await _ctx.Incomes.AnyAsync(i => i.SiteId == id && i.UserId == Uid);
    public async Task TransferExpenses(int from, int? to) { var items = await _ctx.Expenses.Where(e => e.SiteId == from && e.UserId == Uid).ToListAsync(); foreach (var e in items) e.SiteId = to; await _ctx.SaveChangesAsync(); }
    public async Task TransferIncomes(int from, int? to) { var items = await _ctx.Incomes.Where(i => i.SiteId == from && i.UserId == Uid).ToListAsync(); foreach (var i in items) i.SiteId = to; await _ctx.SaveChangesAsync(); }
    public async Task<Site?> GetDefaultSite() => await _ctx.Sites.FirstOrDefaultAsync(s => s.UserId == Uid && s.IsDefault);
    public async Task UpdateDefaultSiteName(string name) { var s = await _ctx.Sites.FirstOrDefaultAsync(x => x.UserId == Uid && x.IsDefault); if (s != null) { s.Name = name; await _ctx.SaveChangesAsync(); } }

    public async Task<List<Category>> GetCategories() => await _ctx.Categories.Where(c => c.UserId == Uid).Include(c => c.Subcategories).OrderBy(c => c.Name).ToListAsync();
    public async Task AddCategory(string n) { _ctx.Categories.Add(new Category { Name = n, UserId = Uid }); await _ctx.SaveChangesAsync(); }
    public async Task UpdateCategory(int id, string name) { var c = await _ctx.Categories.FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid); if (c != null) { c.Name = name; await _ctx.SaveChangesAsync(); } }
    public async Task DeleteCategory(int id) { var c = await _ctx.Categories.Include(x => x.Subcategories).FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid); if (c != null) { _ctx.Categories.Remove(c); await _ctx.SaveChangesAsync(); } }
    public async Task AddSub(int catId, string n) { _ctx.Subcategories.Add(new Subcategory { Name = n, CategoryId = catId }); await _ctx.SaveChangesAsync(); }
    public async Task UpdateSub(int id, string name) { var s = await _ctx.Subcategories.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id && x.Category.UserId == Uid); if (s != null) { s.Name = name; await _ctx.SaveChangesAsync(); } }
    public async Task DeleteSub(int id) { var s = await _ctx.Subcategories.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id && x.Category.UserId == Uid); if (s != null) { _ctx.Subcategories.Remove(s); await _ctx.SaveChangesAsync(); } }

    public async Task<List<Expense>> GetExpenses(int? siteId = null) { var q = _ctx.Expenses.Where(e => e.UserId == Uid).Include(e => e.Site).Include(e => e.Category).Include(e => e.Subcategory).AsQueryable(); if (siteId.HasValue) q = q.Where(e => e.SiteId == siteId.Value); return await q.OrderByDescending(e => e.Date).ToListAsync(); }
    public async Task AddExpense(Expense e) { e.UserId = Uid; _ctx.Expenses.Add(e); await _ctx.SaveChangesAsync(); }
    public async Task UpdateExpense(Expense e) { var ex = await _ctx.Expenses.FirstOrDefaultAsync(x => x.Id == e.Id && x.UserId == Uid); if (ex != null) { ex.Amount = e.Amount; ex.Description = e.Description; ex.Date = e.Date; ex.SiteId = e.SiteId; ex.CategoryId = e.CategoryId; ex.SubcategoryId = e.SubcategoryId; await _ctx.SaveChangesAsync(); } }
    public async Task<Expense?> GetExpenseById(int id) => await _ctx.Expenses.Include(e => e.Site).Include(e => e.Category).Include(e => e.Subcategory).FirstOrDefaultAsync(e => e.Id == id && e.UserId == Uid);
    public async Task DeleteExpense(int id) { var e = await _ctx.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid); if (e != null) { _ctx.Expenses.Remove(e); await _ctx.SaveChangesAsync(); } }

    public async Task<string?> GetSetting(string key) => await _ctx.UserSettings.Where(s => s.UserId == Uid && s.Key == key).Select(s => s.Value).FirstOrDefaultAsync();
    public async Task SetSetting(string key, string value) { var s = await _ctx.UserSettings.FirstOrDefaultAsync(x => x.UserId == Uid && x.Key == key); if (s != null) s.Value = value; else _ctx.UserSettings.Add(new UserSetting { UserId = Uid, Key = key, Value = value }); await _ctx.SaveChangesAsync(); }
}

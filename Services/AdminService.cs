using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ConstructionFinance.Data;
using ConstructionFinance.Models;
namespace ConstructionFinance.Services;

public class AdminUserRow
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsAdmin { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public int SitesCount { get; set; }
    public int ExpensesCount { get; set; }
    public int IncomesCount { get; set; }
    public double IncomeTotal { get; set; }
    public double ExpenseTotal { get; set; }
    public double Balance => IncomeTotal - ExpenseTotal;
}

public class AdminOpResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
}

public class AdminService
{
    private readonly AppDbContext _ctx;
    private readonly UserManager<AppUser> _um;

    public AdminService(AppDbContext ctx, UserManager<AppUser> um) { _ctx = ctx; _um = um; }

    // Дата регистрации в Identity не хранится — используем дату создания системного
    // объекта "Общие расходы", который создаётся автосидом сразу при регистрации.
    public async Task<List<AdminUserRow>> GetUsers()
    {
        var users = await _ctx.Users.OrderBy(u => u.Email).ToListAsync();
        var adminIds = (await _um.GetUsersInRoleAsync(DbInit.AdminRole)).Select(u => u.Id).ToHashSet();

        var rows = new List<AdminUserRow>();
        foreach (var u in users)
        {
            var incomeTotal = await _ctx.Incomes.Where(i => i.UserId == u.Id).SumAsync(i => (double?)i.Amount) ?? 0;
            var expenseTotal = await _ctx.Expenses.Where(e => e.UserId == u.Id).SumAsync(e => (double?)e.Amount) ?? 0;
            var incomesCount = await _ctx.Incomes.CountAsync(i => i.UserId == u.Id);
            var expensesCount = await _ctx.Expenses.CountAsync(e => e.UserId == u.Id);
            var sitesCount = await _ctx.Sites.CountAsync(s => s.UserId == u.Id && !s.IsDefault);
            var registeredAt = await _ctx.Sites.Where(s => s.UserId == u.Id && s.IsDefault)
                .Select(s => (DateTime?)s.StartDate).FirstOrDefaultAsync();

            rows.Add(new AdminUserRow
            {
                Id = u.Id,
                Email = u.Email ?? u.UserName ?? "",
                IsAdmin = adminIds.Contains(u.Id),
                RegisteredAt = registeredAt,
                SitesCount = sitesCount,
                ExpensesCount = expensesCount,
                IncomesCount = incomesCount,
                IncomeTotal = incomeTotal,
                ExpenseTotal = expenseTotal
            });
        }
        return rows;
    }

    public async Task<AdminOpResult> CreateUser(string email, string password)
    {
        email = email.Trim();
        if (string.IsNullOrWhiteSpace(email)) return new AdminOpResult { Error = "Укажите email" };
        if (await _um.FindByEmailAsync(email) != null)
            return new AdminOpResult { Error = "Пользователь с таким email уже есть" };

        var user = new AppUser { UserName = email, Email = email };
        var r = await _um.CreateAsync(user, password);
        if (!r.Succeeded)
            return new AdminOpResult { Error = string.Join("; ", r.Errors.Select(e => e.Description)) };

        await DbInit.CreateDefaults(_ctx, user.Id);
        return new AdminOpResult { Ok = true };
    }

    public async Task<AdminOpResult> ChangeEmail(string userId, string newEmail)
    {
        newEmail = newEmail.Trim();
        if (string.IsNullOrWhiteSpace(newEmail)) return new AdminOpResult { Error = "Укажите email" };

        var user = await _um.FindByIdAsync(userId);
        if (user == null) return new AdminOpResult { Error = "Пользователь не найден" };

        var existing = await _um.FindByEmailAsync(newEmail);
        if (existing != null && existing.Id != userId)
            return new AdminOpResult { Error = "Этот email уже занят другим пользователем" };

        await _um.SetEmailAsync(user, newEmail);
        var r = await _um.SetUserNameAsync(user, newEmail);
        if (!r.Succeeded)
            return new AdminOpResult { Error = string.Join("; ", r.Errors.Select(e => e.Description)) };

        return new AdminOpResult { Ok = true };
    }

    public async Task<AdminOpResult> ResetPassword(string userId, string newPassword)
    {
        var user = await _um.FindByIdAsync(userId);
        if (user == null) return new AdminOpResult { Error = "Пользователь не найден" };

        var token = await _um.GeneratePasswordResetTokenAsync(user);
        var r = await _um.ResetPasswordAsync(user, token, newPassword);
        if (!r.Succeeded)
            return new AdminOpResult { Error = string.Join("; ", r.Errors.Select(e => e.Description)) };

        return new AdminOpResult { Ok = true };
    }

    public async Task<AdminOpResult> SetAdmin(string userId, bool isAdmin, string currentUserId)
    {
        if (userId == currentUserId && !isAdmin)
            return new AdminOpResult { Error = "Нельзя снять права администратора с самого себя" };

        var user = await _um.FindByIdAsync(userId);
        if (user == null) return new AdminOpResult { Error = "Пользователь не найден" };

        var already = await _um.IsInRoleAsync(user, DbInit.AdminRole);
        if (isAdmin && !already) await _um.AddToRoleAsync(user, DbInit.AdminRole);
        else if (!isAdmin && already) await _um.RemoveFromRoleAsync(user, DbInit.AdminRole);

        return new AdminOpResult { Ok = true };
    }

    // Удаляет пользователя и все его данные вручную, в явном порядке — не полагаемся
    // на каскадные удаления на уровне SQLite, т.к. схема БД создана EnsureCreatedAsync
    // и её реальные ON DELETE правила не гарантированы для всех связей.
    public async Task<AdminOpResult> DeleteUser(string userId, string currentUserId)
    {
        if (userId == currentUserId)
            return new AdminOpResult { Error = "Нельзя удалить свою учётную запись" };

        var user = await _um.FindByIdAsync(userId);
        if (user == null) return new AdminOpResult { Error = "Пользователь не найден" };

        using var tx = await _ctx.Database.BeginTransactionAsync();
        try
        {
            _ctx.Expenses.RemoveRange(_ctx.Expenses.Where(e => e.UserId == userId));
            _ctx.Incomes.RemoveRange(_ctx.Incomes.Where(i => i.UserId == userId));
            await _ctx.SaveChangesAsync();

            var catIds = await _ctx.Categories.Where(c => c.UserId == userId).Select(c => c.Id).ToListAsync();
            _ctx.Subcategories.RemoveRange(_ctx.Subcategories.Where(sub => catIds.Contains(sub.CategoryId)));
            await _ctx.SaveChangesAsync();

            _ctx.Categories.RemoveRange(_ctx.Categories.Where(c => c.UserId == userId));
            _ctx.Sites.RemoveRange(_ctx.Sites.Where(s => s.UserId == userId));
            _ctx.UserSettings.RemoveRange(_ctx.UserSettings.Where(s => s.UserId == userId));
            await _ctx.SaveChangesAsync();

            var r = await _um.DeleteAsync(user);
            if (!r.Succeeded)
            {
                await tx.RollbackAsync();
                return new AdminOpResult { Error = string.Join("; ", r.Errors.Select(e => e.Description)) };
            }

            await tx.CommitAsync();
            return new AdminOpResult { Ok = true };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return new AdminOpResult { Error = "Удаление прервано, изменения откачены: " + ex.Message };
        }
    }
}

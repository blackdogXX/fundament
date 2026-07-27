using ConstructionFinance.Models;
using Microsoft.EntityFrameworkCore;
namespace ConstructionFinance.Data;
public static class DbInit
{
    public static async Task InitAsync(IServiceProvider sp)
    {
        using var s = sp.CreateScope();
        var ctx = s.ServiceProvider.GetRequiredService<AppDbContext>();
        await ctx.Database.MigrateAsync();
    }
    public static async Task CreateDefaults(AppDbContext ctx, string uid)
    {
        if (!await ctx.Categories.AnyAsync(c => c.UserId == uid))
        {
            var data = new Dictionary<string, string[]> {
                ["Материалы"] = new[]{"Бетон","Арматура","Кирпич","Пиломатериалы","Кровля","Отделка"},
                ["Техника"] = new[]{"Кран","Экскаватор","Бетононасос","Грузовой транспорт"},
                ["Работы"] = new[]{"Фундамент","Кладка","Электрика","Сантехника","Отделка"},
                ["Транспорт"] = new[]{"Доставка","Вывоз мусора"},
                ["Инструмент"] = new[]{"Ручной","Электро","Расходники"},
                ["Прочее"] = new[]{"Охрана","Уборка","Непредвиденные"}
            };
            foreach (var kv in data) {
                var c = new Category { Name = kv.Key, UserId = uid }; ctx.Categories.Add(c);
                await ctx.SaveChangesAsync();
                foreach (var sub in kv.Value) ctx.Subcategories.Add(new Subcategory { Name = sub, CategoryId = c.Id });
            }
            await ctx.SaveChangesAsync();
        }

        if (!await ctx.Sites.AnyAsync(s => s.UserId == uid && s.IsDefault))
        {
            ctx.Sites.Add(new Site { Name = "Общие расходы", IsDefault = true, UserId = uid, Status = SiteStatus.Active });
            await ctx.SaveChangesAsync();
        }
    }
}

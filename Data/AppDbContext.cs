using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ConstructionFinance.Models;
namespace ConstructionFinance.Data;
public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) {}
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Subcategory>().HasIndex(s => new { s.CategoryId, s.Name }).IsUnique();
        b.Entity<Category>().HasMany(c => c.Subcategories).WithOne(s => s.Category).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Site>().HasMany(s => s.Expenses).WithOne(e => e.Site).OnDelete(DeleteBehavior.Cascade);
    }
}

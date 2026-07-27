using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ConstructionFinance.Models;
public class Category
{
    public int Id { get; set; }
    [Required] [StringLength(100)] public string Name { get; set; } = "";
    [Required] public string UserId { get; set; } = "";
    [ForeignKey("UserId")] public AppUser User { get; set; } = null!;
    public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
}
public class Subcategory
{
    public int Id { get; set; }
    [Required] [StringLength(100)] public string Name { get; set; } = "";
    public int CategoryId { get; set; }
    [ForeignKey("CategoryId")] public Category Category { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
public class Expense
{
    public int Id { get; set; }
    [Required] public double Amount { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public int? SiteId { get; set; }
    [ForeignKey("SiteId")] public Site? Site { get; set; }
    public int? CategoryId { get; set; }
    [ForeignKey("CategoryId")] public Category? Category { get; set; }
    public int? SubcategoryId { get; set; }
    [ForeignKey("SubcategoryId")] public Subcategory? Subcategory { get; set; }
    [Required] public string UserId { get; set; } = "";
    [ForeignKey("UserId")] public AppUser User { get; set; } = null!;
}

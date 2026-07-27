using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ConstructionFinance.Models;
public class Site
{
    public int Id { get; set; }
    [Required] [StringLength(200)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Address { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
    public SiteStatus Status { get; set; } = SiteStatus.Active;
    public bool IsDefault { get; set; } = false;
    [Required] public string UserId { get; set; } = "";
    [ForeignKey("UserId")] public AppUser User { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
public enum SiteStatus { Active, Completed, Paused }

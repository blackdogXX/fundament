using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ConstructionFinance.Models;
public class Income
{
    public int Id { get; set; }
    [Required] public double Amount { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public int? SiteId { get; set; }
    [ForeignKey("SiteId")] public Site? Site { get; set; }
    [Required] public string UserId { get; set; } = "";
    [ForeignKey("UserId")] public AppUser User { get; set; } = null!;
}

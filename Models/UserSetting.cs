using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ConstructionFinance.Models;
public class UserSetting
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = "";
    [Required] [StringLength(50)] public string Key { get; set; } = "";
    [StringLength(500)] public string? Value { get; set; }
    [ForeignKey("UserId")] public AppUser User { get; set; } = null!;
}

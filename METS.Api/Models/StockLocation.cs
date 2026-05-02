using System.ComponentModel.DataAnnotations;

namespace METS.Api.Models;

public class StockLocation
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public ICollection<ReplenishmentRequest> Requests { get; set; } = new List<ReplenishmentRequest>();
}

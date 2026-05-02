using System.ComponentModel.DataAnnotations;

namespace METS.Api.Models;

public class RequestLineItem
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string ArticleNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public decimal RequestedQuantity { get; set; }

    [MaxLength(20)]
    public string Unit { get; set; } = "pcs";

    // Populated when fulfilled
    public decimal? FulfilledQuantity { get; set; }

    // Relation
    public int ReplenishmentRequestId { get; set; }
    public ReplenishmentRequest ReplenishmentRequest { get; set; } = null!;
}

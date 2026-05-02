using System.ComponentModel.DataAnnotations;

namespace METS.Api.Models;

public class ReplenishmentRequest
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Draft;
    public RequestPriority Priority { get; set; } = RequestPriority.Normal;

    // Stock validation (async result stored here after background check)
    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Pending;
    public string? ValidationMessage { get; set; }

    // Rejection
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }

    // Relations
    public int StockLocationId { get; set; }
    public StockLocation StockLocation { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public AppUser CreatedBy { get; set; } = null!;

    public int? ReviewedByUserId { get; set; }
    public AppUser? ReviewedBy { get; set; }

    public ICollection<RequestLineItem> LineItems { get; set; } = new List<RequestLineItem>();
}

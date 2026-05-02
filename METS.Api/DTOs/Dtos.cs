using METS.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace METS.Api.DTOs;

// ── Shared ──────────────────────────────────────────────────────────────────

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// ── Line Items ───────────────────────────────────────────────────────────────

public record LineItemDto(
    int Id,
    string ArticleNumber,
    string Description,
    decimal RequestedQuantity,
    string Unit,
    decimal? FulfilledQuantity
);

public record CreateLineItemDto(
    [Required, MaxLength(50)]  string ArticleNumber,
    [Required, MaxLength(200)] string Description,
    [Range(0.01, double.MaxValue)] decimal RequestedQuantity,
    [MaxLength(20)] string Unit = "pcs"
);

public record FulfillLineItemDto(
    int LineItemId,
    [Range(0, double.MaxValue)] decimal FulfilledQuantity
);

// ── Stock Locations ──────────────────────────────────────────────────────────

public record StockLocationDto(int Id, string Code, string Name, string? Description);

// ── Users ────────────────────────────────────────────────────────────────────

public record UserDto(int Id, string Name, string Username, string Role);

// ── Requests — Summary (list view) ──────────────────────────────────────────

public record RequestSummaryDto(
    int Id,
    string Title,
    string Status,
    string Priority,
    string ValidationStatus,
    string Location,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt
);

// ── Requests — Detail (single view) ─────────────────────────────────────────

public record RequestDetailDto(
    int Id,
    string Title,
    string? Notes,
    string Status,
    string Priority,
    string ValidationStatus,
    string? ValidationMessage,
    string? RejectionReason,
    StockLocationDto Location,
    UserDto CreatedBy,
    UserDto? ReviewedBy,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    DateTime? FulfilledAt,
    IReadOnlyList<LineItemDto> LineItems
);

// ── Requests — Create / Update ───────────────────────────────────────────────

public record CreateRequestDto(
    [Required, MaxLength(200)] string Title,
    [MaxLength(500)] string? Notes,
    RequestPriority Priority,
    [Range(1, int.MaxValue)] int StockLocationId,
    [Range(1, int.MaxValue)] int CreatedByUserId,
    [MinLength(1)] List<CreateLineItemDto> LineItems
);

public record UpdateRequestDto(
    [Required, MaxLength(200)] string Title,
    [MaxLength(500)] string? Notes,
    RequestPriority Priority,
    [Range(1, int.MaxValue)] int StockLocationId,
    [MinLength(1)] List<CreateLineItemDto> LineItems
);

// ── Workflow Actions ─────────────────────────────────────────────────────────

public record SubmitRequestDto([Range(1, int.MaxValue)] int SubmittedByUserId);

public record ApproveRequestDto([Range(1, int.MaxValue)] int ReviewerUserId);

public record RejectRequestDto(
    [Range(1, int.MaxValue)] int ReviewerUserId,
    [Required, MaxLength(500)] string Reason
);

public record FulfillRequestDto([MinLength(1)] List<FulfillLineItemDto> FulfilledItems);

// ── Validation polling ───────────────────────────────────────────────────────

public record ValidationResultDto(string ValidationStatus, string? ValidationMessage);

// ── Filters ──────────────────────────────────────────────────────────────────

public record RequestFilterDto(
    RequestStatus? Status = null,
    RequestPriority? Priority = null,
    int? LocationId = null,
    int Page = 1,
    int PageSize = 20
);

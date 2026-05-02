
namespace METS.Blazor.Models;

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public record StockLocationDto(int Id, string Code, string Name, string? Description);
public record UserDto(int Id, string Name, string Username, string Role);

public record LineItemDto(
    int Id, string ArticleNumber, string Description,
    decimal RequestedQuantity, string Unit, decimal? FulfilledQuantity);

public record RequestSummaryDto(
    int Id, string Title, string Status, string Priority,
    string ValidationStatus, string Location, string CreatedBy,
    DateTime CreatedAt, DateTime? SubmittedAt, DateTime? ReviewedAt);

public record RequestDetailDto(
    int Id, string Title, string? Notes,
    string Status, string Priority,
    string ValidationStatus, string? ValidationMessage,
    string? RejectionReason,
    StockLocationDto Location,
    UserDto CreatedBy, UserDto? ReviewedBy,
    DateTime CreatedAt, DateTime? SubmittedAt,
    DateTime? ReviewedAt, DateTime? FulfilledAt,
    List<LineItemDto> LineItems);

public record ValidationResultDto(string ValidationStatus, string? ValidationMessage);

// --- Form models ---

public class CreateRequestForm
{
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
	public string Priority { get; set; } = "Normal";
	public int StockLocationId { get; set; }
	public int CreatedByUserId { get; set; }
	public List<LineItemForm> LineItems { get; set; } = [new()];
}

public class LineItemForm
{
    public string ArticleNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; } = 1;
    public string Unit { get; set; } = "pcs";
}

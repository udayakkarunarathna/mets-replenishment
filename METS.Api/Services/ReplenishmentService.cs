using METS.Api.BackgroundServices;
using METS.Api.Data;
using METS.Api.DTOs;
using METS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace METS.Api.Services;

public interface IReplenishmentService
{
    Task<PagedResult<RequestSummaryDto>> GetRequestsAsync(RequestFilterDto filter);
    Task<RequestDetailDto?> GetRequestByIdAsync(int id);
    Task<RequestDetailDto> CreateRequestAsync(CreateRequestDto dto);
    Task<RequestDetailDto?> UpdateRequestAsync(int id, UpdateRequestDto dto);
    Task<(bool Success, string? Error, RequestDetailDto? Result)> SubmitRequestAsync(int id, SubmitRequestDto dto);
    Task<(bool Success, string? Error, RequestDetailDto? Result)> ApproveRequestAsync(int id, ApproveRequestDto dto);
    Task<(bool Success, string? Error, RequestDetailDto? Result)> RejectRequestAsync(int id, RejectRequestDto dto);
    Task<(bool Success, string? Error, RequestDetailDto? Result)> FulfillRequestAsync(int id, FulfillRequestDto dto);
    Task<ValidationResultDto?> GetValidationStatusAsync(int id);
}

public class ReplenishmentService(
    MetsDbContext db,
    StockValidationQueue validationQueue,
    ILogger<ReplenishmentService> logger) : IReplenishmentService
{
    // ── Query ────────────────────────────────────────────────────────────────

    public async Task<PagedResult<RequestSummaryDto>> GetRequestsAsync(RequestFilterDto filter)
    {
        var query = db.ReplenishmentRequests
            .Include(r => r.StockLocation)
            .Include(r => r.CreatedBy)
            .AsNoTracking()
            .AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);
        if (filter.Priority.HasValue)
            query = query.Where(r => r.Priority == filter.Priority.Value);
        if (filter.LocationId.HasValue)
            query = query.Where(r => r.StockLocationId == filter.LocationId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => r.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<RequestSummaryDto>(items, total, filter.Page, filter.PageSize);
    }

    public async Task<RequestDetailDto?> GetRequestByIdAsync(int id)
    {
        var request = await LoadFullRequestAsync(id);
        return request?.ToDetailDto();
    }

    public async Task<ValidationResultDto?> GetValidationStatusAsync(int id)
    {
        var request = await db.ReplenishmentRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null) return null;
        return new ValidationResultDto(request.ValidationStatus.ToString(), request.ValidationMessage);
    }

    // ── Create / Update ──────────────────────────────────────────────────────

    public async Task<RequestDetailDto> CreateRequestAsync(CreateRequestDto dto)
    {
        var request = new ReplenishmentRequest
        {
            Title            = dto.Title,
            Notes            = dto.Notes,
            Priority         = dto.Priority,
            Status           = RequestStatus.Draft,
            ValidationStatus = ValidationStatus.Pending,
            StockLocationId  = dto.StockLocationId,
            CreatedByUserId  = dto.CreatedByUserId,
            CreatedAt        = DateTime.UtcNow,
            LineItems        = dto.LineItems.Select(l => new RequestLineItem
            {
                ArticleNumber     = l.ArticleNumber,
                Description       = l.Description,
                RequestedQuantity = l.RequestedQuantity,
                Unit              = l.Unit
            }).ToList()
        };

        db.ReplenishmentRequests.Add(request);
        await db.SaveChangesAsync();

        return (await LoadFullRequestAsync(request.Id))!.ToDetailDto();
    }

    public async Task<RequestDetailDto?> UpdateRequestAsync(int id, UpdateRequestDto dto)
    {
        var request = await db.ReplenishmentRequests
            .Include(r => r.LineItems)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null) return null;
        if (request.Status != RequestStatus.Draft)
            throw new InvalidOperationException("Only Draft requests can be edited.");

        request.Title           = dto.Title;
        request.Notes           = dto.Notes;
        request.Priority        = dto.Priority;
        request.StockLocationId = dto.StockLocationId;

        // Replace line items
        db.RequestLineItems.RemoveRange(request.LineItems);
        request.LineItems = dto.LineItems.Select(l => new RequestLineItem
        {
            ArticleNumber     = l.ArticleNumber,
            Description       = l.Description,
            RequestedQuantity = l.RequestedQuantity,
            Unit              = l.Unit
        }).ToList();

        await db.SaveChangesAsync();
        return (await LoadFullRequestAsync(id))!.ToDetailDto();
    }

    // ── Workflow ─────────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error, RequestDetailDto? Result)> SubmitRequestAsync(
        int id, SubmitRequestDto dto)
    {
        var request = await db.ReplenishmentRequests
            .Include(r => r.LineItems)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
            return (false, "Request not found.", null);

        if (request.Status != RequestStatus.Draft)
            return (false, $"Cannot submit a request in '{request.Status}' status.", null);

        if (!request.LineItems.Any())
            return (false, "Cannot submit a request with no line items.", null);

        request.Status           = RequestStatus.Submitted;
        request.ValidationStatus = ValidationStatus.Pending;
        request.SubmittedAt      = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Enqueue async stock check — returns immediately to caller
        await validationQueue.Writer.WriteAsync(new StockValidationWorkItem(id));
        logger.LogInformation("Request {Id} submitted; stock validation enqueued.", id);

        return (true, null, (await LoadFullRequestAsync(id))!.ToDetailDto());
    }

    public async Task<(bool Success, string? Error, RequestDetailDto? Result)> ApproveRequestAsync(
        int id, ApproveRequestDto dto)
    {
        var request = await db.ReplenishmentRequests.FindAsync(id);
        if (request is null)
            return (false, "Request not found.", null);

        if (request.Status != RequestStatus.Submitted)
            return (false, $"Cannot approve a request in '{request.Status}' status.", null);

        var reviewer = await db.Users.FindAsync(dto.ReviewerUserId);
        if (reviewer is null || reviewer.Role != UserRole.Reviewer)
            return (false, "Reviewer not found or user does not have Reviewer role.", null);

        request.Status           = RequestStatus.Approved;
        request.ReviewedByUserId = dto.ReviewerUserId;
        request.ReviewedAt       = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (true, null, (await LoadFullRequestAsync(id))!.ToDetailDto());
    }

    public async Task<(bool Success, string? Error, RequestDetailDto? Result)> RejectRequestAsync(
        int id, RejectRequestDto dto)
    {
        var request = await db.ReplenishmentRequests.FindAsync(id);
        if (request is null)
            return (false, "Request not found.", null);

        if (request.Status != RequestStatus.Submitted)
            return (false, $"Cannot reject a request in '{request.Status}' status.", null);

        var reviewer = await db.Users.FindAsync(dto.ReviewerUserId);
        if (reviewer is null || reviewer.Role != UserRole.Reviewer)
            return (false, "Reviewer not found or user does not have Reviewer role.", null);

        request.Status           = RequestStatus.Rejected;
        request.RejectionReason  = dto.Reason;
        request.ReviewedByUserId = dto.ReviewerUserId;
        request.ReviewedAt       = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (true, null, (await LoadFullRequestAsync(id))!.ToDetailDto());
    }

    public async Task<(bool Success, string? Error, RequestDetailDto? Result)> FulfillRequestAsync(
        int id, FulfillRequestDto dto)
    {
        var request = await db.ReplenishmentRequests
            .Include(r => r.LineItems)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
            return (false, "Request not found.", null);

        if (request.Status != RequestStatus.Approved)
			//return (false, $"Cannot fulfill a request in '{request.Status}' status.", null);
			return (false, "Request must be in 'Approved' status to be fulfilled.", null);

		foreach (var fulfillDto in dto.FulfilledItems)
        {
            var lineItem = request.LineItems.FirstOrDefault(l => l.Id == fulfillDto.LineItemId);
            if (lineItem is not null)
                lineItem.FulfilledQuantity = fulfillDto.FulfilledQuantity;
        }

        request.Status      = RequestStatus.Fulfilled;
        request.FulfilledAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (true, null, (await LoadFullRequestAsync(id))!.ToDetailDto());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ReplenishmentRequest?> LoadFullRequestAsync(int id) =>
        await db.ReplenishmentRequests
            .Include(r => r.StockLocation)
            .Include(r => r.CreatedBy)
            .Include(r => r.ReviewedBy)
            .Include(r => r.LineItems)
            .FirstOrDefaultAsync(r => r.Id == id);
}

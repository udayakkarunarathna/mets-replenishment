using METS.Api.Models;

namespace METS.Api.DTOs;

public static class Mappers
{
    public static StockLocationDto ToDto(this StockLocation loc) =>
        new(loc.Id, loc.Code, loc.Name, loc.Description);

    public static UserDto ToDto(this AppUser user) =>
        new(user.Id, user.Name, user.Username, user.Role.ToString());

    public static LineItemDto ToDto(this RequestLineItem item) =>
        new(item.Id, item.ArticleNumber, item.Description,
            item.RequestedQuantity, item.Unit, item.FulfilledQuantity);

    public static RequestSummaryDto ToSummaryDto(this ReplenishmentRequest r) =>
        new(r.Id, r.Title,
            r.Status.ToString(), r.Priority.ToString(), r.ValidationStatus.ToString(),
            r.StockLocation?.Name ?? string.Empty,
            r.CreatedBy?.Name ?? string.Empty,
            r.CreatedAt, r.SubmittedAt, r.ReviewedAt);

    public static RequestDetailDto ToDetailDto(this ReplenishmentRequest r) =>
        new(r.Id, r.Title, r.Notes,
            r.Status.ToString(), r.Priority.ToString(),
            r.ValidationStatus.ToString(), r.ValidationMessage,
            r.RejectionReason,
            r.StockLocation.ToDto(),
            r.CreatedBy.ToDto(),
            r.ReviewedBy?.ToDto(),
            r.CreatedAt, r.SubmittedAt, r.ReviewedAt, r.FulfilledAt,
            r.LineItems.Select(i => i.ToDto()).ToList());
}

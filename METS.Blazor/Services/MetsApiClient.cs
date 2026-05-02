using System.Net.Http.Json;
using METS.Blazor.Models;

namespace METS.Blazor.Services;

public class MetsApiClient(HttpClient http)
{
    // ── Metadata ────────────────────────────────────────────────────────────

    public Task<List<StockLocationDto>?> GetLocationsAsync() =>
        http.GetFromJsonAsync<List<StockLocationDto>>("api/locations");

    public Task<List<UserDto>?> GetUsersAsync(string? role = null)
    {
        var url = role is null ? "api/users" : $"api/users?role={role}";
        return http.GetFromJsonAsync<List<UserDto>>(url);
    }

    // ── Requests ─────────────────────────────────────────────────────────────

    public Task<PagedResult<RequestSummaryDto>?> GetRequestsAsync(
        string? status = null, string? priority = null,
        int? locationId = null, int page = 1, int pageSize = 20)
    {
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (status is not null) qs.Add($"status={status}");
        if (priority is not null) qs.Add($"priority={priority}");
        if (locationId.HasValue) qs.Add($"locationId={locationId}");
        return http.GetFromJsonAsync<PagedResult<RequestSummaryDto>>(
            $"api/requests?{string.Join('&', qs)}");
    }

    public Task<RequestDetailDto?> GetRequestAsync(int id) =>
        http.GetFromJsonAsync<RequestDetailDto>($"api/requests/{id}");

    public Task<ValidationResultDto?> GetValidationAsync(int id) =>
        http.GetFromJsonAsync<ValidationResultDto>($"api/requests/{id}/validation");

    public async Task<RequestDetailDto?> CreateRequestAsync(object payload)
    {
        var resp = await http.PostAsJsonAsync("api/requests", payload);
		var error = await resp.Content.ReadAsStringAsync();
		Console.WriteLine(error);
		resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<RequestDetailDto>();
    }

    public async Task<RequestDetailDto?> SubmitRequestAsync(int id, int userId)
    {
        var resp = await http.PostAsJsonAsync($"api/requests/{id}/submit",
            new { submittedByUserId = userId });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<RequestDetailDto>();
    }

    public async Task<RequestDetailDto?> ApproveRequestAsync(int id, int reviewerId)
    {
        var resp = await http.PostAsJsonAsync($"api/requests/{id}/approve",
            new { reviewerUserId = reviewerId });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<RequestDetailDto>();
    }

    public async Task<RequestDetailDto?> RejectRequestAsync(int id, int reviewerId, string reason)
    {
        var resp = await http.PostAsJsonAsync($"api/requests/{id}/reject",
            new { reviewerUserId = reviewerId, reason });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<RequestDetailDto>();
    }

    public async Task<RequestDetailDto?> FulfillRequestAsync(int id, List<object> fulfilledItems)
    {
        var resp = await http.PostAsJsonAsync($"api/requests/{id}/fulfill",
            new { fulfilledItems });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<RequestDetailDto>();
    }
}

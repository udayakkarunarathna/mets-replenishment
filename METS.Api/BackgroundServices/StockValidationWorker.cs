using System.Threading.Channels;
using METS.Api.Data;
using METS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace METS.Api.BackgroundServices;

/// <summary>
/// Queued item submitted for stock validation.
/// </summary>
public record StockValidationWorkItem(int RequestId);

/// <summary>
/// Singleton channel that accepts validation work items from the API layer.
/// </summary>
public class StockValidationQueue
{
    private readonly Channel<StockValidationWorkItem> _channel =
        Channel.CreateUnbounded<StockValidationWorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public ChannelWriter<StockValidationWorkItem> Writer => _channel.Writer;
    public ChannelReader<StockValidationWorkItem> Reader => _channel.Reader;
}

/// <summary>
/// Hosted background service that reads from the queue and performs
/// the slow external stock availability check, then persists the result.
/// </summary>
public class StockValidationWorker(
    StockValidationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<StockValidationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StockValidationWorker started.");

        await foreach (var item in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(item, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing stock validation for request {Id}", item.RequestId);
            }
        }
    }

    private async Task ProcessAsync(StockValidationWorkItem item, CancellationToken ct)
    {
        logger.LogInformation("Running stock validation for request {Id}...", item.RequestId);

        // Simulate the slow external service (3–8 seconds)
        var delay = Random.Shared.Next(3000, 8000);
        await Task.Delay(delay, ct);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MetsDbContext>();

        var request = await db.ReplenishmentRequests
            .Include(r => r.LineItems)
            .FirstOrDefaultAsync(r => r.Id == item.RequestId, ct);

        if (request is null)
        {
            logger.LogWarning("Request {Id} not found for validation.", item.RequestId);
            return;
        }

        // Only validate if still Submitted (could have been rejected/cancelled in the meantime)
        if (request.Status != RequestStatus.Submitted)
        {
            logger.LogInformation("Request {Id} is no longer Submitted; skipping validation.", item.RequestId);
            return;
        }

        // Simulate: 80% pass, 20% fail
        var passed = Random.Shared.NextDouble() > 0.20;

        if (passed)
        {
            request.ValidationStatus = ValidationStatus.Passed;
            request.ValidationMessage =
                $"All {request.LineItems.Count} item(s) confirmed available in central warehouse.";
        }
        else
        {
            // Pick a random line item to flag as unavailable
            var flagged = request.LineItems.ElementAt(Random.Shared.Next(request.LineItems.Count));
            request.ValidationStatus = ValidationStatus.Failed;
            request.ValidationMessage =
                $"Item '{flagged.ArticleNumber}' has insufficient stock. Requested: {flagged.RequestedQuantity} {flagged.Unit}.";
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Validation complete for request {Id}: {Result}", item.RequestId, request.ValidationStatus);
    }
}

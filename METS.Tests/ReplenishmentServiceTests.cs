using METS.Api.BackgroundServices;
using METS.Api.Data;
using METS.Api.DTOs;
using METS.Api.Models;
using METS.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace METS.Tests;

[TestFixture]
public class ReplenishmentServiceTests
{
    private MetsDbContext _db = null!;
    private StockValidationQueue _queue = null!;
    private IReplenishmentService _sut = null!;

    private const int WorkerId   = 1;
    private const int ReviewerId = 2;
    private const int LocationId = 1;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MetsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new MetsDbContext(options);
        _db.Database.EnsureCreated();
        SeedTestData(_db);

        _queue = new StockValidationQueue();
        var logger = Substitute.For<ILogger<ReplenishmentService>>();
        _sut = new ReplenishmentService(_db, _queue, logger);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── Create ───────────────────────────────────────────────────────────────

    [Test]
    public async Task CreateRequest_ValidDto_ReturnsDraftRequest()
    {
        var dto = new CreateRequestDto(
            Title: "Test Restock",
            Notes: null,
            Priority: RequestPriority.Normal,
            StockLocationId: LocationId,
            CreatedByUserId: WorkerId,
            LineItems: [new CreateLineItemDto("ART-001", "Test Article", 10, "pcs")]
        );

        var result = await _sut.CreateRequestAsync(dto);

        Assert.Multiple(() =>
        {
            Assert.That(result.Title, Is.EqualTo("Test Restock"));
            Assert.That(result.Status, Is.EqualTo("Draft"));
            Assert.That(result.ValidationStatus, Is.EqualTo("Pending"));
            Assert.That(result.LineItems, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task CreateRequest_MultipleLineItems_AllPersisted()
    {
        var dto = new CreateRequestDto(
            Title: "Multi Item Request",
            Notes: null,
            Priority: RequestPriority.Urgent,
            StockLocationId: LocationId,
            CreatedByUserId: WorkerId,
            LineItems:
            [
                new CreateLineItemDto("ART-A", "Article A", 5, "pcs"),
                new CreateLineItemDto("ART-B", "Article B", 10, "kg"),
                new CreateLineItemDto("ART-C", "Article C", 2, "box"),
            ]
        );

        var result = await _sut.CreateRequestAsync(dto);

        Assert.That(result.LineItems, Has.Count.EqualTo(3));
    }

    // ── Submit ───────────────────────────────────────────────────────────────

    [Test]
    public async Task SubmitRequest_DraftWithLineItems_TransitionsToSubmitted()
    {
        var requestId = await CreateDraftRequest();

        var (success, error, result) = await _sut.SubmitRequestAsync(
            requestId, new SubmitRequestDto(WorkerId));

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(result!.Status, Is.EqualTo("Submitted"));
            Assert.That(result.SubmittedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task SubmitRequest_AlreadySubmitted_ReturnsFalse()
    {
        var requestId = await CreateDraftRequest();
        await _sut.SubmitRequestAsync(requestId, new SubmitRequestDto(WorkerId));

        // Try to submit again
        var (success, error, _) = await _sut.SubmitRequestAsync(
            requestId, new SubmitRequestDto(WorkerId));

        Assert.That(success, Is.False);
        Assert.That(error, Does.Contain("Submitted"));
    }

    [Test]
    public async Task SubmitRequest_NonExistentId_ReturnsFalse()
    {
        var (success, error, _) = await _sut.SubmitRequestAsync(
            9999, new SubmitRequestDto(WorkerId));

        Assert.That(success, Is.False);
        Assert.That(error, Does.Contain("not found"));
    }

    [Test]
    public async Task SubmitRequest_EnqueuesValidationWorkItem()
    {
        var requestId = await CreateDraftRequest();

        await _sut.SubmitRequestAsync(requestId, new SubmitRequestDto(WorkerId));

        // Check the queue has an item
        Assert.That(_queue.Reader.TryRead(out var item), Is.True);
        Assert.That(item!.RequestId, Is.EqualTo(requestId));
    }

    // ── Approve ──────────────────────────────────────────────────────────────

    [Test]
    public async Task ApproveRequest_SubmittedRequest_TransitionsToApproved()
    {
        var requestId = await CreateAndSubmitRequest();

        var (success, _, result) = await _sut.ApproveRequestAsync(
            requestId, new ApproveRequestDto(ReviewerId));

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result!.Status, Is.EqualTo("Approved"));
            Assert.That(result.ReviewedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task ApproveRequest_ByWorker_ReturnsFalse()
    {
        var requestId = await CreateAndSubmitRequest();

        // WorkerId has role Worker, not Reviewer
        var (success, error, _) = await _sut.ApproveRequestAsync(
            requestId, new ApproveRequestDto(WorkerId));

        Assert.That(success, Is.False);
        Assert.That(error, Does.Contain("Reviewer"));
    }

    [Test]
    public async Task ApproveRequest_DraftRequest_ReturnsFalse()
    {
        var requestId = await CreateDraftRequest();

        var (success, error, _) = await _sut.ApproveRequestAsync(
            requestId, new ApproveRequestDto(ReviewerId));

        Assert.That(success, Is.False);
        Assert.That(error, Does.Contain("Draft"));
    }

    // ── Reject ───────────────────────────────────────────────────────────────

    [Test]
    public async Task RejectRequest_SubmittedRequest_TransitionsToRejected()
    {
        var requestId = await CreateAndSubmitRequest();

        var (success, _, result) = await _sut.RejectRequestAsync(
            requestId, new RejectRequestDto(ReviewerId, "Missing safety approval."));

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result!.Status, Is.EqualTo("Rejected"));
            Assert.That(result.RejectionReason, Is.EqualTo("Missing safety approval."));
        });
    }

    // ── Fulfill ──────────────────────────────────────────────────────────────

    [Test]
    public async Task FulfillRequest_ApprovedRequest_TransitionsToFulfilled()
    {
        var requestId = await CreateAndSubmitRequest();
        await _sut.ApproveRequestAsync(requestId, new ApproveRequestDto(ReviewerId));

        var request = await _sut.GetRequestByIdAsync(requestId);
        var lineItemId = request!.LineItems[0].Id;

        var (success, _, result) = await _sut.FulfillRequestAsync(requestId, new FulfillRequestDto(
            [new FulfillLineItemDto(lineItemId, 10)]));

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result!.Status, Is.EqualTo("Fulfilled"));
            Assert.That(result.FulfilledAt, Is.Not.Null);
            Assert.That(result.LineItems[0].FulfilledQuantity, Is.EqualTo(10));
        });
    }

    [Test]
    public async Task FulfillRequest_NotApproved_ReturnsFalse()
    {
        var requestId = await CreateAndSubmitRequest();

        var (success, error, _) = await _sut.FulfillRequestAsync(requestId,
            new FulfillRequestDto([]));

        Assert.That(success, Is.False);
        Assert.That(error, Does.Contain("Approved"));
    }

    // ── Filter / Pagination ───────────────────────────────────────────────────

    [Test]
    public async Task GetRequests_FilterByStatus_ReturnsCorrectSubset()
    {
        // Create multiple requests in different statuses
        var draft1 = await CreateDraftRequest();
        var draft2 = await CreateDraftRequest();
        var submitted = await CreateAndSubmitRequest();

        var result = await _sut.GetRequestsAsync(
            new RequestFilterDto(Status: RequestStatus.Draft, Page: 1, PageSize: 50));

        Assert.That(result.Items, Has.All.With.Property("Status").EqualTo("Draft"));
    }

    [Test]
    public async Task GetRequests_Pagination_RespectsPageSize()
    {
        // Create 5 requests
        for (var i = 0; i < 5; i++) await CreateDraftRequest();

        var result = await _sut.GetRequestsAsync(
            new RequestFilterDto(Page: 1, PageSize: 2));

        Assert.That(result.Items, Has.Count.LessThanOrEqualTo(2));
        Assert.That(result.TotalCount, Is.GreaterThanOrEqualTo(5));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> CreateDraftRequest()
    {
        var result = await _sut.CreateRequestAsync(new CreateRequestDto(
            Title: "Test Request",
            Notes: null,
            Priority: RequestPriority.Normal,
            StockLocationId: LocationId,
            CreatedByUserId: WorkerId,
            LineItems: [new CreateLineItemDto("ART-TEST", "Test Article", 10, "pcs")]
        ));
        return result.Id;
    }

    private async Task<int> CreateAndSubmitRequest()
    {
        var id = await CreateDraftRequest();
        await _sut.SubmitRequestAsync(id, new SubmitRequestDto(WorkerId));
        return id;
    }

    private static void SeedTestData(MetsDbContext db)
    {
        db.Users.AddRange(
            new AppUser { Id = WorkerId,   Name = "Test Worker",   Username = "worker",   Role = UserRole.Worker },
            new AppUser { Id = ReviewerId, Name = "Test Reviewer", Username = "reviewer", Role = UserRole.Reviewer }
        );
        db.StockLocations.Add(
            new StockLocation { Id = LocationId, Code = "T-01", Name = "Test Location" }
        );
        db.SaveChanges();
    }
}

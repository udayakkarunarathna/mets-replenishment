using METS.Api.Models;

namespace METS.Api.Data;

public static class DbSeeder
{
    public static void Seed(MetsDbContext db)
    {
        if (db.Users.Any()) return; // Already seeded

        // --- Users ---
        var workers = new[]
        {
            new AppUser { Id = 1, Name = "Udaya Karunarathna",   Username = "udaya",   Role = UserRole.Worker },
            new AppUser { Id = 2, Name = "Ruwani Dharmasiri",    Username = "ruwani",     Role = UserRole.Worker },
            new AppUser { Id = 3, Name = "Yuhas Dewan",   Username = "yuhas",   Role = UserRole.Worker },
        };
        var reviewers = new[]
        {
            new AppUser { Id = 4, Name = "Thehas Methsan",  Username = "thehas",   Role = UserRole.Reviewer },
            new AppUser { Id = 5, Name = "Eranda Suraj",      Username = "eranda",     Role = UserRole.Reviewer },
        };
        db.Users.AddRange(workers);
        db.Users.AddRange(reviewers);

        // --- Locations ---
        var locations = new[]
        {
            new StockLocation { Id = 1, Code = "A-01", Name = "Assembly Line A",     Description = "Primary assembly station" },
            new StockLocation { Id = 2, Code = "A-02", Name = "Assembly Line B",     Description = "Secondary assembly station" },
            new StockLocation { Id = 3, Code = "W-01", Name = "Welding Station 1",   Description = "Main welding bay" },
            new StockLocation { Id = 4, Code = "W-02", Name = "Welding Station 2",   Description = "Auxiliary welding bay" },
            new StockLocation { Id = 5, Code = "P-01", Name = "Packaging Station",   Description = "Final packaging area" },
        };
        db.StockLocations.AddRange(locations);

        // --- Requests ---
        var now = DateTime.UtcNow;

        var requests = new List<ReplenishmentRequest>
        {
            // 1. Draft
            new()
            {
                Id = 1,
                Title = "Restock M8 Bolts",
                Notes = "Running low — need before end of shift",
                Status = RequestStatus.Draft,
                Priority = RequestPriority.Normal,
                ValidationStatus = ValidationStatus.Pending,
                StockLocationId = 1,
                CreatedByUserId = 1,
                CreatedAt = now.AddHours(-2),
                LineItems = new List<RequestLineItem>
                {
                    new() { ArticleNumber = "BLT-M8-25", Description = "M8x25 Hex Bolt, Stainless",    RequestedQuantity = 500, Unit = "pcs" },
                    new() { ArticleNumber = "NUT-M8",    Description = "M8 Hex Nut, Stainless",         RequestedQuantity = 500, Unit = "pcs" },
                }
            },
            // 2. Submitted - validation pending (simulates in-flight check)
            new()
            {
                Id = 2,
                Title = "Welding Wire Replenishment",
                Notes = "Urgent — wire stock critically low",
                Status = RequestStatus.Submitted,
                Priority = RequestPriority.Urgent,
                ValidationStatus = ValidationStatus.Pending,
                StockLocationId = 3,
                CreatedByUserId = 2,
                CreatedAt = now.AddHours(-5),
                SubmittedAt = now.AddMinutes(-10),
                LineItems = new List<RequestLineItem>
                {
                    new() { ArticleNumber = "WW-0.8MM", Description = "MIG Welding Wire 0.8mm 15kg spool", RequestedQuantity = 10, Unit = "spool" },
                    new() { ArticleNumber = "WW-1.0MM", Description = "MIG Welding Wire 1.0mm 15kg spool", RequestedQuantity = 5,  Unit = "spool" },
                }
            },
            // 3. Submitted - validation passed
            new()
            {
                Id = 3,
                Title = "Circuit Board Components",
                Status = RequestStatus.Submitted,
                Priority = RequestPriority.Normal,
                ValidationStatus = ValidationStatus.Passed,
                ValidationMessage = "All 3 items confirmed in stock at central warehouse.",
                StockLocationId = 2,
                CreatedByUserId = 1,
                CreatedAt = now.AddDays(-1),
                SubmittedAt = now.AddHours(-3),
                LineItems = new List<RequestLineItem>
                {
                    new() { ArticleNumber = "PCB-CTL-V2", Description = "Control PCB v2.1",           RequestedQuantity = 20,  Unit = "pcs" },
                    new() { ArticleNumber = "CAP-100UF",  Description = "Electrolytic Cap 100µF 25V", RequestedQuantity = 200, Unit = "pcs" },
                    new() { ArticleNumber = "RES-10K",    Description = "Resistor 10kΩ 0805 SMD",     RequestedQuantity = 500, Unit = "pcs" },
                }
            },
            // 4. Approved
            new()
            {
                Id = 4,
                Title = "Packaging Tape and Labels",
                Status = RequestStatus.Approved,
                Priority = RequestPriority.Low,
                ValidationStatus = ValidationStatus.Passed,
                ValidationMessage = "Stock available.",
                StockLocationId = 5,
                CreatedByUserId = 3,
                ReviewedByUserId = 4,
                CreatedAt = now.AddDays(-2),
                SubmittedAt = now.AddDays(-2).AddHours(2),
                ReviewedAt = now.AddDays(-1),
                LineItems = new List<RequestLineItem>
                {
                    new() { ArticleNumber = "TAPE-48MM", Description = "Clear Packing Tape 48mm",    RequestedQuantity = 50, Unit = "roll" },
                    new() { ArticleNumber = "LBL-A4",   Description = "Shipping Labels A4 Sheet",   RequestedQuantity = 20, Unit = "box"  },
                }
            },
            // 5. Rejected
            new()
            {
                Id = 5,
                Title = "Industrial Solvent Restock",
                Status = RequestStatus.Rejected,
                Priority = RequestPriority.Normal,
                ValidationStatus = ValidationStatus.Failed,
                ValidationMessage = "Item SOL-IPA-5L not found in inventory system.",
                RejectionReason = "Procurement of this item requires safety approval form HS-22. Please resubmit through the chemical request portal.",
                StockLocationId = 3,
                CreatedByUserId = 2,
                ReviewedByUserId = 5,
                CreatedAt = now.AddDays(-3),
                SubmittedAt = now.AddDays(-3).AddHours(1),
                ReviewedAt = now.AddDays(-2),
                LineItems = new List<RequestLineItem>
                {
                    new() { ArticleNumber = "SOL-IPA-5L", Description = "Isopropyl Alcohol 5L",   RequestedQuantity = 4, Unit = "can" },
                }
            },
            // 6. Fulfilled
            new()
            {
                Id = 6,
                Title = "Safety Gloves Restock",
                Status = RequestStatus.Fulfilled,
                Priority = RequestPriority.Normal,
                ValidationStatus = ValidationStatus.Passed,
                ValidationMessage = "All items available.",
                StockLocationId = 1,
                CreatedByUserId = 1,
                ReviewedByUserId = 4,
                CreatedAt = now.AddDays(-5),
                SubmittedAt = now.AddDays(-5).AddHours(1),
                ReviewedAt = now.AddDays(-4),
                FulfilledAt = now.AddDays(-3),
                LineItems = new List<RequestLineItem>
                {
                    new() { ArticleNumber = "GLV-L-NIT",  Description = "Nitrile Gloves Size L (box 100)", RequestedQuantity = 10, Unit = "box",  FulfilledQuantity = 10 },
                    new() { ArticleNumber = "GLV-XL-NIT", Description = "Nitrile Gloves Size XL (box 100)",RequestedQuantity = 5,  Unit = "box",  FulfilledQuantity = 5  },
                }
            },
            // 7. Another Urgent Draft
            new()
            {
                Id = 7,
                Title = "Conveyor Belt Lubricant",
                Notes = "Belt squeaking badly — maintenance needed ASAP",
                Status = RequestStatus.Draft,
                Priority = RequestPriority.Urgent,
                ValidationStatus = ValidationStatus.Pending,
                StockLocationId = 4,
                CreatedByUserId = 3,
                CreatedAt = now.AddMinutes(-30),
                LineItems = new List<RequestLineItem>
                {
                    new() { ArticleNumber = "LUB-CB-1L", Description = "Conveyor Belt Lubricant 1L", RequestedQuantity = 6, Unit = "bottle" },
                }
            },
        };

        db.ReplenishmentRequests.AddRange(requests);
        db.SaveChanges();
    }
}

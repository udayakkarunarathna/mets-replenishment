using METS.Api.BackgroundServices;
using METS.Api.Data;
using METS.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database — SQLite in-memory with shared connection (persists for app lifetime) ──
var keepAliveConnection = new SqliteConnection("DataSource=:memory:");
keepAliveConnection.Open();

builder.Services.AddDbContext<MetsDbContext>(options =>
    options.UseSqlite(keepAliveConnection));

// ── Background validation queue + worker ────────────────────────────────────
builder.Services.AddSingleton<StockValidationQueue>();
builder.Services.AddHostedService<StockValidationWorker>();

// ── Application services ────────────────────────────────────────────────────
builder.Services.AddScoped<IReplenishmentService, ReplenishmentService>();

// ── API ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Converters.Add(
			new System.Text.Json.Serialization.JsonStringEnumConverter());
	});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "METS Stock Replenishment API", Version = "v1" });
});

// ── CORS — allow Blazor dev server ──────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("BlazorPolicy", policy =>
        policy.WithOrigins("http://localhost:5001", "https://localhost:7001")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// ── Seed database ────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MetsDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

// ── Middleware ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "METS API v1"));
}

app.UseCors("BlazorPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();

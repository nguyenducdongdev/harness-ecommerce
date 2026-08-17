using System.Reflection;
using FluentValidation;
using Hangfire;
using Harness.Api.Persistence;
using Harness.BuildingBlocks.Application.Behaviors;
using Harness.BuildingBlocks.Infrastructure;
using Harness.BuildingBlocks.Presentation.Middleware;
using Harness.Modules.Catalog;
using Harness.Modules.Catalog.Application.Abstractions;
using Harness.Modules.Catalog.Infrastructure.Search;
using Harness.Modules.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

// ===== Assembly của tất cả module (MediatR + FluentValidation scan) =====
var moduleAssemblies = new[]
{
    Assembly.Load("Harness.Modules.Catalog"),
    Assembly.Load("Harness.Modules.Order"),
    Assembly.Load("Harness.Modules.Inventory"),
    Assembly.Load("Harness.Modules.Customer"),
    Assembly.Load("Harness.Modules.Promotion"),
    Assembly.Load("Harness.Modules.Payment"),
    Assembly.Load("Harness.Modules.Shipping"),
    Assembly.Load("Harness.Modules.Loyalty"),
    Assembly.Load("Harness.Modules.Review"),
    Assembly.Load("Harness.Modules.Cms"),
    Assembly.Load("Harness.Modules.Integration")
};

var builder = WebApplication.CreateBuilder(args);

// ===== Logging: Serilog → console + Seq =====
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// ===== Database: PostgreSQL multi-schema =====
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:PostgreSQL trong cấu hình.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsHistoryTable("__migrations", "shared")));

// Kiểm tra readonly interface cho handler các module
builder.Services.AddScoped<Harness.BuildingBlocks.Infrastructure.Persistence.IHarnessDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// ===== BuildingBlocks: Redis cache + RabbitMQ event bus =====
builder.Services.AddBuildingBlocksInfrastructure(builder.Configuration);

// ===== Khởi tạo module (DI riêng của từng module) =====
builder.Services.AddCatalogModule(builder.Configuration);
builder.Services.AddShippingModule(builder.Configuration);
builder.Services.AddPaymentModule(builder.Configuration);
builder.Services.AddCustomerModule(builder.Configuration);

// ===== MediatR (CQRS) + pipeline behaviors =====
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(moduleAssemblies);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// ===== FluentValidation =====
builder.Services.AddValidatorsFromAssemblies(moduleAssemblies);

// ===== MVC controllers (tự động scan controller trong assembly tham chiếu) =====
builder.Services.AddControllers();

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Harness Ecommerce API",
        Version = "v1",
        Description = "Nền tảng bán nội thất ứng dụng chuỗi cửa hàng — .NET 8 Modular Monolith. " +
                      "Sẽ mở rộng mobile app, ERP, DMS, sản xuất qua cùng API này."
    });
});

// ===== Hangfire: background jobs (Outbox publisher, sync jobs) =====
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(pg => pg.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

// ===== Health checks =====
builder.Services.AddHealthChecks()
    .AddNpgsql(connectionString, name: "postgresql")
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis");

var app = builder.Build();

// ===== Pipeline =====
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Harness Ecommerce API v1");
    });

    // Migration + seed tự động (chỉ Development — Production chạy migration qua CI/CD)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CatalogSeed.SeedAsync(db);

        // Khởi tạo index Elasticsearch (best-effort — không ném lỗi nếu ES chưa sẵn sàng)
        var indexer = scope.ServiceProvider.GetRequiredService<IProductIndexer>();
        await indexer.EnsureIndexAsync();
    }
}

app.UseAuthorization();
app.MapControllers();
app.MapHangfireDashboard("/hangfire");
app.MapHealthChecks("/health");

// Outbox publisher: mỗi phút publish các integration event chưa gửi
RecurringJob.AddOrUpdate<OutboxPublisherJob>(
    "outbox-publisher",
    job => job.PublishPendingAsync(CancellationToken.None),
    Cron.Minutely());

// Reindex sản phẩm lên Elasticsearch mỗi ngày (đảm bảo index đồng bộ dữ liệu thay đổi/seed)
RecurringJob.AddOrUpdate<ProductReindexJob>(
    "catalog-product-reindex",
    job => job.RunAsync(CancellationToken.None),
    Cron.Daily());

app.Run();

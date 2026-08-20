using Harness.Api.Hubs;
using System.Reflection;

using System.Text;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Harness.Api.Persistence;
using Harness.BuildingBlocks.Application.Behaviors;
using Harness.BuildingBlocks.Infrastructure;
using Harness.BuildingBlocks.Infrastructure.Events;
using Harness.BuildingBlocks.Presentation.Middleware;
using Harness.Modules.Auth;
using Harness.Modules.Auth.Infrastructure;
using Harness.Modules.Catalog;
using Harness.Modules.Catalog.Application.Abstractions;
using Harness.Modules.Catalog.Infrastructure;
using Harness.Modules.Catalog.Infrastructure.Search;
using Harness.Modules.Customer;
using Harness.Modules.Inventory;
using Harness.Modules.Integration.Infrastructure;
using Harness.Modules.Integration.Application;
using Harness.Modules.Payment;
using Harness.Modules.Shipping;
using Harness.Api.Observability;
using Harness.Api.Reporting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
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
    Assembly.Load("Harness.Modules.Integration"),
    Assembly.Load("Harness.Modules.Auth")
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
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);

// ===== Integration/ERP: handlers đồng bộ + processor + consumer RabbitMQ =====
builder.Services.Configure<ErpOptions>(builder.Configuration.GetSection(ErpOptions.SectionName));
builder.Services.AddScoped<IErpSyncHandler, ErpOrderSyncHandler>();
builder.Services.AddScoped<IErpSyncHandler, ErpOrderStatusSyncHandler>();
builder.Services.AddScoped<IErpSyncHandler, ErpPaymentSyncHandler>();
builder.Services.AddScoped<ErpSyncProcessor>();
builder.Services.AddHostedService<ErpSyncHostedService>();

// ===== JWT Authentication + Authorization (admin RBAC) =====
var authSection = builder.Configuration.GetSection("Auth");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authSection["Issuer"] ?? "harness-api",
            ValidateAudience = true,
            ValidAudience = authSection["Audience"] ?? "harness-admin",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(authSection["SecretKey"] ?? "harness-dev-secret-key-change-me-0123456789")),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

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
builder.Services.AddSignalR();


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

// ===== Observability: Prometheus /metrics + business metrics reporter =====
builder.Services.Configure<MetricsOptions>(builder.Configuration.GetSection(MetricsOptions.SectionName));
builder.Services.AddSingleton<HarnessMetrics>();
builder.Services.AddScoped<MetricsReporter>();
builder.Services.AddHostedService<MetricsReporterHostedService>();

// ===== Reporting/Dashboard: query nặng bằng Dapper (Phase 3) =====
builder.Services.AddScoped<DashboardQueries>();

// ===== Health checks =====
var rabbitMqOptions = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql")
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis")
    .AddRabbitMQ(
        $"amqp://{rabbitMqOptions.UserName}:{rabbitMqOptions.Password}@{rabbitMqOptions.HostName}:{rabbitMqOptions.Port}",
        name: "rabbitmq");

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
        await LoyaltySeed.SeedAsync(db);
        await CmsSeed.SeedAsync(db);
        await AuthSeed.SeedAsync(db, builder.Configuration.GetSection("Auth").Get<JwtOptions>() ?? new());

        // Khởi tạo index Elasticsearch (best-effort — không ném lỗi nếu ES chưa sẵn sàng)
        var indexer = scope.ServiceProvider.GetRequiredService<IProductIndexer>();
        await indexer.EnsureIndexAsync();
    }
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpMetrics();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.MapHangfireDashboard("/hangfire");
app.MapHealthChecks("/health");
app.MapMetrics();

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

namespace Harness.Modules.Catalog.Infrastructure.Search;

/// <summary>Hangfire job: build lại toàn bộ chỉ mục sản phẩm từ DB (recurring hằng ngày).</summary>
public class ProductReindexJob
{
    private readonly ProductReindexService _service;

    public ProductReindexJob(ProductReindexService service) => _service = service;

    public Task RunAsync(CancellationToken cancellationToken = default)
        => _service.ReindexAllAsync(cancellationToken);
}

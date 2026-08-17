using Harness.Modules.Catalog.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace Harness.Modules.Catalog.Infrastructure.Search;

public class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";
    public string Uri { get; set; } = "http://localhost:9200";
    public string IndexProducts { get; set; } = "products";
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Indexer + tìm kiếm sản phẩm trên Elasticsearch bằng NEST.
/// Mọi thao tác đều best-effort: nếu ES không sẵn sàng, log cảnh báo và không
/// làm hỏng luồng nghiệp vụ (Catalog vẫn hoạt động qua EF + JSONB).
/// </summary>
public class ProductIndexer : IProductIndexer, IProductSearch
{
    private readonly ElasticClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<ProductIndexer> _logger;
    private bool _indexReady;

    public ProductIndexer(IOptions<ElasticsearchOptions> options, ILogger<ProductIndexer> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new ElasticClient(new ConnectionSettings(new Uri(_options.Uri))
            .DefaultIndex(_options.IndexProducts));
    }

    public async Task EnsureIndexAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled || _indexReady) return;

        try
        {
            var exists = await _client.Indices.ExistsAsync(_options.IndexProducts, ct);
            if (!exists.Exists)
            {
                await _client.Indices.CreateAsync(_options.IndexProducts, c => c
                    .Map<ProductSearchDocument>(m => m.AutoMap()
                        .Properties(ps => ps
                            .Text(t => t.Name(p => p.Name).Fields(f => f.Keyword(k => k.Name("raw"))))
                            .Text(t => t.Name(p => p.Slug).Fields(f => f.Keyword(k => k.Name("raw"))))
                            .Text(t => t.Name(p => p.Sku).Fields(f => f.Keyword(k => k.Name("raw"))))
                            .Text(t => t.Name(p => p.ShortDescription))
                            .Text(t => t.Name(p => p.Description))
                            .Text(t => t.Name(p => p.CategoryName))
                            .Keyword(t => t.Name(p => p.CategorySlug))
                            .Text(t => t.Name(p => p.BrandName))
                            .Text(t => t.Name(p => p.Attributes))
                            .Text(t => t.Name(p => p.Tags))
                            .Date(d => d.Name(p => p.CreatedAt)))), ct);
            }
            _indexReady = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Elasticsearch khởi tạo index thất bại (retry ở thao tác sau): {Error}", ex.Message);
            _indexReady = false;
        }
    }

    public async Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default)
        => await RunBestEffortAsync(async c =>
        {
            await EnsureIndexAsync(c);
            var resp = await _client.IndexAsync(document, d => d.Index(_options.IndexProducts), c);
            return resp.IsValid;
        }, ct);

    public async Task IndexManyAsync(IEnumerable<ProductSearchDocument> documents, CancellationToken ct = default)
    {
        var docs = documents.ToList();
        if (docs.Count == 0) return;

        await RunBestEffortAsync(async c =>
        {
            await EnsureIndexAsync(c);
            var resp = await _client.BulkAsync(b => b.Index(_options.IndexProducts).IndexMany(docs), c);
            return resp.IsValid && !resp.Errors;
        }, ct);
    }

    public async Task RemoveProductAsync(int productId, CancellationToken ct = default)
        => await RunBestEffortAsync(async c =>
        {
            var resp = await _client.DeleteAsync<ProductSearchDocument>(
                productId, d => d.Index(_options.IndexProducts), c);
            return resp.IsValid || resp.Result == Result.NotFound;
        }, ct);

    public async Task<IReadOnlyList<ProductSearchDocument>> SearchProductsAsync(
        string term, int from = 0, int size = 20, CancellationToken ct = default)
    {
        if (!_options.Enabled) return Array.Empty<ProductSearchDocument>();

        try
        {
            var resp = await _client.SearchAsync<ProductSearchDocument>(s => s
                .Index(_options.IndexProducts)
                .From(Math.Max(0, from))
                .Size(Math.Clamp(size, 1, 100))
                .Query(q => q.Exists(e => e.Field(f => f.Id)) && BuildQuery(term)), ct);

            return resp.IsValid ? resp.Documents.ToList() : new List<ProductSearchDocument>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Elasticsearch tìm kiếm thất bại (trả về rỗng): {Error}", ex.Message);
            return Array.Empty<ProductSearchDocument>();
        }
    }

    private QueryContainer BuildQuery(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return new QueryContainerDescriptor<ProductSearchDocument>().MatchAll();

        return new QueryContainerDescriptor<ProductSearchDocument>()
            .MultiMatch(m => m
                .Fields(f => f
                    .Field(p => p.Name, 3)
                    .Field(p => p.Sku, 2)
                    .Field(p => p.Description)
                    .Field(p => p.Attributes, 1.5)
                    .Field(p => p.CategoryName)
                    .Field(p => p.BrandName)
                    .Field(p => p.Tags))
                .Query(term)
                .Fuzziness(Fuzziness.Auto));
    }

    private async Task RunBestEffortAsync(Func<CancellationToken, Task<bool>> action, CancellationToken ct)
    {
        if (!_options.Enabled) return;
        try
        {
            var ok = await action(ct);
            if (!ok) _logger.LogWarning("Elasticsearch thao tác không thành công (response không hợp lệ).");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Elasticsearch thao tác thất bại: {Error}", ex.Message);
            _indexReady = false;
        }
    }
}

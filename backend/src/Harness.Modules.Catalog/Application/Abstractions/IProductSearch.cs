namespace Harness.Modules.Catalog.Application.Abstractions;

/// <summary>Tra cứu sản phẩm theo full-text trên Elasticsearch.</summary>
public interface IProductSearch
{
    /// <summary>Tìm kiếm theo từ khóa (name/description/attributes), có phân trang (from/size).</summary>
    Task<IReadOnlyList<ProductSearchDocument>> SearchProductsAsync(
        string term, int from = 0, int size = 20, CancellationToken cancellationToken = default);
}

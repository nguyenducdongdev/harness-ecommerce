namespace Harness.Modules.Catalog.Application.Abstractions;

/// <summary>Quản lý chỉ mục sản phẩm trên Elasticsearch (create/update/remove/reindex).</summary>
public interface IProductIndexer
{
    /// <summary>Đảm bảo index tồn tại với mapping đúng (best-effort, không ném lỗi nếu ES chưa sẵn sàng).</summary>
    Task EnsureIndexAsync(CancellationToken cancellationToken = default);

    Task IndexProductAsync(ProductSearchDocument document, CancellationToken cancellationToken = default);

    Task IndexManyAsync(IEnumerable<ProductSearchDocument> documents, CancellationToken cancellationToken = default);

    Task RemoveProductAsync(int productId, CancellationToken cancellationToken = default);
}

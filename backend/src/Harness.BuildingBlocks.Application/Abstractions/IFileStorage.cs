namespace Harness.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstraction for file storage (local filesystem hoặc MinIO/S3).
/// Module chỉ phụ thuộc interface, implementation đăng ký tại composition root.
/// </summary>
public interface IFileStorage
{
    /// <summary>Lưu file từ stream, trả về public URL tương đối (ví dụ: /uploads/products/image-xxx.jpg).</summary>
    Task<string> SaveAsync(string category, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Xóa file theo URL tương đối.</summary>
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>Lấy public URL đầy đủ (scheme+host) từ URL tương đối.</summary>
    string GetPublicUrl(string relativePath);
}

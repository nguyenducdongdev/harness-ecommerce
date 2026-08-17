using Harness.BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Harness.BuildingBlocks.Infrastructure.Storage;

/// <summary>
/// File storage lưu trên local filesystem (phục vụ Development + staging).
/// Production: thay bằng MinioFileStorage implement cùng IFileStorage.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly LocalFileStorageOptions _options;
    private readonly string _basePath;

    public LocalFileStorage(IOptions<LocalFileStorageOptions> options)
    {
        _options = options.Value;
        _basePath = _options.BasePath ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(string category, string fileName, Stream content, string contentType,
        CancellationToken cancellationToken = default)
    {
        var safeCategory = Sanitize(category);
        var safeFileName = $"{Guid.NewGuid():N}_{Sanitize(fileName)}";
        var relativePath = $"/{safeCategory}/{safeFileName}";
        var fullPath = Path.Combine(_basePath, safeCategory);

        Directory.CreateDirectory(fullPath);
        var filePath = Path.Combine(fullPath, safeFileName);

        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return relativePath;
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, relativePath.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string relativePath)
    {
        var baseUrl = _options.PublicUrl?.TrimEnd('/') ?? "/uploads";
        return $"{baseUrl}{relativePath}";
    }

    private static string Sanitize(string input)
    {
        return input.Replace("..", "").Replace("/", "").Replace("\\", "").Trim();
    }
}

public class LocalFileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string? BasePath { get; set; }
    public string? PublicUrl { get; set; }
}

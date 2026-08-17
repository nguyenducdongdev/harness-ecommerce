using Harness.BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Harness.BuildingBlocks.Infrastructure.Storage;

public class MinioStorageOptions
{
    public const string SectionName = "MinIO";
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "harness";
    public string SecretKey { get; set; } = "harness123";
    public string Bucket { get; set; } = "harness";
    public string PublicUrl { get; set; } = "http://localhost:9000";
    public bool UseSsl { get; set; }
}

/// <summary>
/// File storage lên MinIO (S3-compatible) — dùng cho Production.
/// Chọn provider qua cấu hình: FileStorage:Provider = "minio" | "local".
/// Object được lưu theo cấu trúc: /{bucket}/{category}/{key}.
/// </summary>
public class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly MinioStorageOptions _options;

    public MinioFileStorage(IOptions<MinioStorageOptions> options)
    {
        _options = options.Value;
        _client = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();
    }

    public async Task<string> SaveAsync(string category, string fileName, Stream content, string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        var safeCategory = Sanitize(category);
        var key = $"{safeCategory}/{Guid.NewGuid():N}_{Sanitize(fileName)}";
        var relativePath = $"/{key}";

        Stream seekable = content;
        long size;
        if (content.CanSeek && content.Length > 0)
        {
            seekable = content;
            size = content.Length;
        }
        else
        {
            var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            seekable = buffer;
            size = buffer.Length;
        }

        var putArgs = new PutObjectArgs()
            .WithBucket(_options.Bucket)
            .WithObject(key)
            .WithStreamData(seekable)
            .WithObjectSize(size)
            .WithContentType(contentType);

        await _client.PutObjectAsync(putArgs, cancellationToken);
        return relativePath;
    }

    public async Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var objectName = relativePath.TrimStart('/');
        await _client.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(_options.Bucket).WithObject(objectName), cancellationToken);
    }

    public string GetPublicUrl(string relativePath)
        => $"{_options.PublicUrl.TrimEnd('/')}/{_options.Bucket}/{relativePath.TrimStart('/')}";

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_options.Bucket), cancellationToken);
        if (!exists)
        {
            await _client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_options.Bucket), cancellationToken);
        }
    }

    private static string Sanitize(string input)
        => input.Replace("..", "").Replace("/", "").Replace("\\", "").Trim();
}

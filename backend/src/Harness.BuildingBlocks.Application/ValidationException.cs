namespace Harness.BuildingBlocks.Application;

/// <summary>Exception validation ứng dụng — middleware sẽ trả về 400 kèm danh sách lỗi.</summary>
public sealed class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base("Dữ liệu gửi lên không hợp lệ.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}

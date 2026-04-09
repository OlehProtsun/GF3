namespace BusinessLogicLayer.Common;

/// <summary>
/// Structured result of a delete operation that may fail for expected business reasons.
/// This allows services and controllers to return friendly validation feedback without using exceptions
/// for dependency-related delete guards.
/// </summary>
public sealed class DeleteOperationResult
{
    private const string GeneralErrorKey = "general";
    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new Dictionary<string, string[]>(0, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Indicates whether the delete operation completed successfully.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Human-readable explanation used when the delete operation fails for a business reason.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Structured field-level or general errors suitable for validation-style HTTP responses.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } = EmptyErrors;

    /// <summary>
    /// Creates a successful delete result with no attached errors.
    /// </summary>
    public static DeleteOperationResult Success() => new()
    {
        Succeeded = true,
    };

    /// <summary>
    /// Creates a failed delete result with one general validation-style message.
    /// </summary>
    public static DeleteOperationResult Failure(string message)
        => new()
        {
            Succeeded = false,
            Message = message,
            Errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [GeneralErrorKey] = [message],
            },
        };
}

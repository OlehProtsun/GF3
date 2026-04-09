namespace BusinessLogicLayer.Common;

/// <summary>
/// Domain-level validation exception used when business rules reject a request.
/// The exception can carry field-specific errors so higher layers can build structured validation
/// responses instead of falling back to one generic message.
/// </summary>
public sealed class ValidationException : Exception
{
    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new Dictionary<string, string[]>(0, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Field-level validation errors associated with the exception.
    /// The dictionary may also contain a generic key when the validation issue is not tied to one field.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Creates a validation exception with only a general message.
    /// </summary>
    public ValidationException(string message)
        : this(message, errors: null)
    {
    }

    /// <summary>
    /// Creates a validation exception with a general message and optional field-level details.
    /// </summary>
    public ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors)
        : base(message)
    {
        Errors = NormalizeErrors(errors);
    }

    /// <summary>
    /// Creates a validation exception tied to one specific field.
    /// </summary>
    public static ValidationException ForField(string field, string message)
        => new(
            message,
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [field] = [message],
            });

    private static IReadOnlyDictionary<string, string[]> NormalizeErrors(IReadOnlyDictionary<string, string[]>? errors)
    {
        if (errors is null)
        {
            return EmptyErrors;
        }

        return errors
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
            .Select(entry => new KeyValuePair<string, string[]>(
                entry.Key,
                entry.Value
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray()))
            .Where(entry => entry.Value.Length > 0)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
    }
}

namespace BusinessLogicLayer.Security;

/// <summary>
/// Single authoritative policy for account passwords across login, creation, updates, and recovery.
/// </summary>
public static class NumericPasswordPolicy
{
    public const int Length = 6;
    public const string ValidationMessage = "Password must contain exactly 6 digits.";

    public static bool IsValid(string? password)
        => password is not null &&
           password.Length == Length &&
           password.All(character => character is >= '0' and <= '9');
}

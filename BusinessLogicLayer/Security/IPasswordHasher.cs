namespace BusinessLogicLayer.Security;

/// <summary>
/// Small abstraction around password hashing so auth code stays easy to test.
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}

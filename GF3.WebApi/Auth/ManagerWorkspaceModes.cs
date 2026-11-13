namespace WebApi.Auth;

public static class ManagerWorkspaceModes
{
    public const string Choose = "choose";
    public const string Pc = "pc";
    public const string Phone = "phone";
    public const string ClaimType = "manager_workspace_mode";

    public static bool IsSelectable(string? value) => value is Pc or Phone;

    public static string ResolveForManager(string? signedValue) => signedValue switch
    {
        null => Pc,
        Choose or Pc or Phone => signedValue,
        _ => throw new ArgumentException("Invalid manager workspace mode.", nameof(signedValue)),
    };
}

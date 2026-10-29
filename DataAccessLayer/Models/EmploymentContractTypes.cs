namespace DataAccessLayer.Models;

public static class EmploymentContractTypes
{
    public const string Zlecenie = "zlecenie";
    public const string OPrace = "o_prace";

    public static bool IsValid(string? value) => value is Zlecenie or OPrace;
}

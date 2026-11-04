namespace WebApi.Contracts.ShiftSwaps;

public sealed class CreateContractClaimApprovalRequest
{
    public int ShiftSwapRequestId { get; set; }
    public string? FromTime { get; set; }
    public string? ToTime { get; set; }
}

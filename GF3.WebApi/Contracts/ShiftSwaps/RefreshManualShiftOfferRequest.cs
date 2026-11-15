using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.ShiftSwaps;

public sealed class RefreshManualShiftOfferRequest
{
    [Required]
    public DateTimeOffset? ExpectedPublishedAtUtc { get; set; }
}

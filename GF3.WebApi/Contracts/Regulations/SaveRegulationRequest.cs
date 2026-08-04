using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Regulations;

public sealed class SaveRegulationRequest
{
    [Required]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Version { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;

    public IFormFile? Pdf { get; set; }
}

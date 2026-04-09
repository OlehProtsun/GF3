using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.Slots;

/// <summary>
/// Request payload used to replace the full slot collection of a graph in one batch.
/// </summary>
public sealed class ReplaceGraphSlotsRequest
{
    [Required]
    public List<CreateGraphSlotRequest> Slots { get; set; } = [];
}

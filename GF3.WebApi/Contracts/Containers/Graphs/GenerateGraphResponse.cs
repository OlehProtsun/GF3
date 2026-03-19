using WebApi.Contracts.Containers.Graphs.Slots;

namespace WebApi.Contracts.Containers.Graphs;

public sealed class GenerateGraphResponse
{
    public int ContainerId { get; set; }
    public int GraphId { get; set; }
    public int GeneratedSlotsCount { get; set; }
    public int WrittenSlotsCount { get; set; }
    public IEnumerable<GraphSlotDto>? Slots { get; set; }
}

namespace WebApi.Contracts.Containers;

public sealed class ContainerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
}

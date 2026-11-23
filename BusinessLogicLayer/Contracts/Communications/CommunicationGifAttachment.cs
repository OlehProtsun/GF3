namespace BusinessLogicLayer.Contracts.Communications;

public sealed record CommunicationGifAttachment(Guid Key, string FileName, int Size)
{
    public string ContentType { get; init; } = "image/gif";
}

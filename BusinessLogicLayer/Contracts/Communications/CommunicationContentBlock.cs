namespace BusinessLogicLayer.Contracts.Communications;

// The position of a block in the list is its display order.
public sealed record CommunicationContentBlock(
    Guid Id, string Type, string? Text, CommunicationGifAttachment? Media);

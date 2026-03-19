namespace BusinessLogicLayer.Contracts.Shops
{
    public sealed class ShopDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
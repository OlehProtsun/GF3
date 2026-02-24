namespace BusinessLogicLayer.Contracts.Shops
{
    public sealed class SaveShopRequest
    {
        public int Id { get; set; } // 0 = create, >0 = update
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
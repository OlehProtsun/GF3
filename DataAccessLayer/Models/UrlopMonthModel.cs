namespace DataAccessLayer.Models;

public sealed class UrlopMonthModel
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public bool IsOpen { get; set; }
    public int Revision { get; set; }
}

namespace APBD_cw9.Models;

public class ProductAtWarehouseInfo
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
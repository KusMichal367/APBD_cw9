namespace APBD_cw9.Services;

public interface IDbService
{
    Task <int> AddProductAsync(int idProduct, int idWarehouse, int amount, DateTime createdAt);
}
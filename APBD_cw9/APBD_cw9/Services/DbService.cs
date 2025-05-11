using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace APBD_cw9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;

    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProductAsync(int idProduct, int idWarehouse, int amount, DateTime createdAt)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await using SqlCommand command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            //1
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than 0");
            }

            command.CommandText = "Select Count(*) from Product where IdProduct = @idProduct";
            command.Parameters.AddWithValue("@idProduct", idProduct);

            var productCount = (int)await command.ExecuteScalarAsync();
            if (productCount == 0)
            {
                throw new InvalidOperationException($"Product {idProduct} does not exist");
            }

            command.Parameters.Clear();

            command.CommandText = "Select Count(*) from Warehouse where IdWarehouse = @idWarehouse";
            command.Parameters.AddWithValue("@idWarehouse", idWarehouse);

            var warehouseCount = (int)await command.ExecuteScalarAsync();
            if (warehouseCount == 0)
            {
                throw new InvalidOperationException($"Warehouse {idWarehouse} does not exist");
            }

            command.Parameters.Clear();


            //2
            command.CommandText =
                "SELECT Count(*) from [Order] where IdProduct = @idProduct and Amount = @amount and CreatedAt < CAST(@createdAt AS DATETIME)";
            command.Parameters.AddWithValue("@idProduct", idProduct);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@createdAt", createdAt);

            var orderCount = (int)await command.ExecuteScalarAsync();
            if (orderCount == 0)
            {
                throw new InvalidOperationException($"Order for product {idProduct} in amount {amount} does not exist");
            }

            command.Parameters.Clear();

            //3
            command.CommandText =
                "SELECT IdOrder from [Order] where IdProduct = @idProduct and Amount = @amount and CreatedAt < CAST(@createdAt AS DATETIME)";
            command.Parameters.AddWithValue("@idProduct", idProduct);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@createdAt", createdAt);

            int orderId = (int)await command.ExecuteScalarAsync();

            command.Parameters.Clear();

            command.CommandText = "SELECT Count(*) from Product_Warehouse where IdOrder = @idOrder";
            command.Parameters.AddWithValue("@idOrder", orderId);
            var product_WarehouseCount = (int)await command.ExecuteScalarAsync();

            if (product_WarehouseCount > 0)
            {
                throw new InvalidOperationException("Order has been fulfilled");
            }

            command.Parameters.Clear();

            //4
            command.CommandText = "UPDATE [Order] set FulfilledAt = @now where IdOrder = @orderId";
            command.Parameters.AddWithValue("@orderId", orderId);
            command.Parameters.AddWithValue("@now", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();

            command.Parameters.Clear();
            //5
            command.CommandText = "SELECT price from Product where IdProduct = @idProduct";
            command.Parameters.AddWithValue("@idProduct", idProduct);

            var price = (decimal)await command.ExecuteScalarAsync();
            command.Parameters.Clear();

            command.CommandText =
                "INSERT into Product_Warehouse output INSERTED.IdProductWarehouse values (@WarehouseId,@ProductId,@OrderId,@Amount,@Price,@CreatedAt)";
            command.Parameters.AddWithValue("@WarehouseId", idWarehouse);
            command.Parameters.AddWithValue("@ProductId", idProduct);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@Price", price * amount);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            var newId= (int) await command.ExecuteNonQueryAsync();
            command.Parameters.Clear();

            await transaction.CommitAsync();
            return newId;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
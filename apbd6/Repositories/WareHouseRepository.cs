using System.Data;
using WebApplication2.Exceptions;

namespace apbd6.Services;

using Microsoft.Data.SqlClient;


public interface IWarehouseRepository
{
    public Task<int?> RegisterProductAsync(int idProduct, int idWarehouse, int amount);
    public Task ProcRegisterProductAsync(int idProduct, int idWarehouse, int amount);
    public Task<bool> CheckProductByIdAsync(int? productId);
    public Task<bool> CheckWarehouseByIdAsync(int? productId);
    public Task<bool> CheckOrderCorrectnessAsync(int? productId, int? amount);
    public Task<bool> CheckFulfilled(int idOrder);
    public Task<double> GetOrderCostAsync(int idProduct, int idOrder);
    public Task<int> GetPriceAsync(int idProduct);
    public Task<int> GetAmountAsync(int idOrder);
}

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IConfiguration _iConfiguration;

    public WarehouseRepository(IConfiguration iConfiguration)
    {
        _iConfiguration = iConfiguration;
    }

    public async Task<int?> RegisterProductAsync(int IdProduct, int IdWarehouse, int amount)
    {
        await using var connection = new SqlConnection(_iConfiguration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var query = "UPDATE \"Order\" SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder";
            await using var command = new SqlCommand(query, connection);
            command.Transaction = (SqlTransaction)transaction;
            command.Parameters.AddWithValue("@IdOrder", IdProduct);
            await command.ExecuteNonQueryAsync();

            command.CommandText = @"INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, CreatedAt, Ammount, Price) OUTPUT Inserted.IdProductWarehouse Values (@IdWarehouse, @IdProduct, @IdOrder, GETDATE(), @Ammount, @Price);";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@IdWarehouse", IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", IdProduct);
            command.Parameters.AddWithValue("@IdOrder", amount);
            command.Parameters.AddWithValue("Ammount", await GetAmountAsync(amount));
            command.Parameters.AddWithValue("Price", await GetOrderCostAsync(IdProduct, amount));
            var idProductWarehouse = (int)await command.ExecuteScalarAsync();
            await transaction.CommitAsync();
            return idProductWarehouse;

        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }

    public async Task ProcRegisterProductAsync(int idProduct, int idWarehouse, int amount)
    {
        await using var connection = new SqlConnection(_iConfiguration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        await using var command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("IdProduct", idProduct);
        command.Parameters.AddWithValue("IdWarehouse",idWarehouse);
        command.Parameters.AddWithValue("Amount", amount);
        command.Parameters.AddWithValue("CreatedAt", DateTime.Now);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> CheckProductByIdAsync(int? productId)
    {
        await using var connection = new SqlConnection(_iConfiguration["ConfigurationStrings:DefaultConnection"]);
        await connection.OpenAsync();
        var query = "SELECT * FROM Product WHERE IdProduct = @IdProduct";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdProduct", productId);
        return (int) await command.ExecuteScalarAsync() > 0;
    }

    public async Task<bool> CheckWarehouseByIdAsync(int? productId)
    {
        await using var connection = new SqlConnection(_iConfiguration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        var query = "SELECT * FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdWarehouse", productId);
        return (int)await command.ExecuteScalarAsync()>0;
    }

    public async Task<bool> CheckOrderCorrectnessAsync(int? productId, int? amount)
    {
        await using var connection = new SqlConnection(_iConfiguration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        var query = "SELECT * FROM \"Order\" WHERE IdProduct = @IdProduct AND Ammount = @Ammount";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdProduct", productId);
        command.Parameters.AddWithValue("@Ammount", amount);
        return (int)await command.ExecuteScalarAsync()>0;
    }
    
    public async Task<bool> CheckFulfilled(int idOrder)
    {
        await using var connection = new SqlConnection(_iConfiguration["ConnectionStrings:DefaultString"]);
        await connection.OpenAsync();
        var query = "SELECT * FROM Product_Warehouse WHERE IdOrder = @IdOrder";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        return (int)await command.ExecuteScalarAsync() > 0;
    }

    
    //fetching private utility methods
    public async Task<double> GetOrderCostAsync(int idProduct, int idOrder)
    {
        int price = await GetPriceAsync(idProduct);
        int ammount = await GetAmountAsync(idOrder);

        return price * ammount;
    }

    public async Task<int> GetPriceAsync(int idProduct)
    {
        await using var connection = new SqlConnection(_iConfiguration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        var query = "SELECT Price FROM Product WHERE @IdProduct = IdProduct";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdProduct", idProduct);
        var reader = await command.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            await reader.ReadAsync();
            return reader.GetInt32(reader.GetOrdinal("Price"));
        }
        else
        {
            throw new ConflictException("No product with id " + idProduct);
        }
        
    }
    public async Task<int> GetAmountAsync(int idOrder)
    {
        await using var connnection = new SqlConnection(_iConfiguration["ConnectionStrings:DefaultConnection"]);
        await connnection.OpenAsync();
        var query = "SELECT Ammount FROM \"Order\" WHERE @idOrder = IdOrder";
        await using var command = new SqlCommand(query, connnection);
        var reader = await command.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            await reader.ReadAsync();
            return reader.GetInt32(reader.GetOrdinal("Ammount"));
        }
        else
        {
            throw new ConflictException("no order with id " + idOrder + " exists");
        }
    }
}

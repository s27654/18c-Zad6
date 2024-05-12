using System;
using System.Data;
using System.Data.SqlClient;
using WebApplication2.Models;

namespace WebApplication2.Repositories;

public interface IWarehouseRepository
{
    Task<bool> CheckProductExistsAsync(int idProduct);
    Task<bool> CheckWarehouseExistsAsync(int idWarehouse);
    Task<bool> CheckProductInWarehouseAsync(int idProduct, int idOrder);
    Task<ProductWarehouse> GetOrderIfExistsAsync(int idProduct, int amount, DateTime createdAt);    public Task<int?> RegisterProductInWarehouseAsync(int idWarehouse, int idProduct, int idOrder, DateTime createdAt);
    public Task RegisterProductInWarehouseByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt);
}

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IConfiguration _configuration;
    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> CheckProductExistsAsync(int idProduct)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings;DefaultConnection"]);
        await connection.OpenAsync();
        var query = "SELECT COUNT(1) FROM Product WHERE IdProduct = @IdProduct";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdProduct", idProduct);
        var exists = (int)await command.ExecuteScalarAsync() > 0;
        return exists;
    }
    public async Task<bool> CheckWarehouseExistsAsync(int idWarehouse)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings;DefaultConnection"]);
        await connection.OpenAsync();
        var query = "SELECT COUNT(1) FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
        var exists = (int)await command.ExecuteScalarAsync() > 0;
        return exists;
    }
    public async Task<bool> CheckProductInWarehouseAsync(int idProduct, int idOrder)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings;DefaultConnection"]);
        await connection.OpenAsync();
        var query = "SELECT COUNT(1) FROM Product_Warehouse WHERE IdProduct = @IdProduct AND IdOrder = @IdOrder";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdProduct", idProduct);
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        var exists = (int)await command.ExecuteScalarAsync() > 0;
        return exists;
    }

    public async Task<ProductWarehouse> GetOrderIfExistsAsync(int idProduct, int amount, DateTime createdAt)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings;DefaultConnection"]);
        await connection.OpenAsync();
        var query =
            "SELECT TOP 1  IdProductWarehouse, IdWarehouse, IdProduct, IdOrder, CrearedAt FROM Product_Warehouse WHERE IdProduct = @IdProduct AND Amount = @Amount AND  CreatedAt < @CreatedAt ORDER BY CretedAt DESC";
        
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdProduct", idProduct);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@CreatedAt", createdAt);
        await using var read = await command.ExecuteReaderAsync();
        if (await read.ReadAsync())
        {
            return new ProductWarehouse
            {
                IdProductWarehouse = read.GetInt32(0),
                IdWarehouse = read.GetInt32(1),
                IdOrder = read.GetInt32(3),
                CreatedAt = read.GetDateTime(4)
            };
        }
        return null;
    }
    public async Task<int?> RegisterProductInWarehouseAsync(int idWarehouse, int idProduct, int idOrder, DateTime createdAt)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var query = "UPDATE \"Order\" SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
            await using var command = new SqlCommand(query, connection);
            command.Transaction = (SqlTransaction)transaction;
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@FulfilledAt", DateTime.UtcNow);
            await command.ExecuteNonQueryAsync();
            
            command.CommandText = @"
                      INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, CreatedAt, Amount, Price)
                      OUTPUT Inserted.IdProductWarehouse
                      VALUES (@IdWarehouse, @IdProduct, @IdOrder, @CreatedAt, 0, 0);";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
            command.Parameters.AddWithValue("@IdProduct", idProduct);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);
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
    
    public async Task RegisterProductInWarehouseByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        await using var command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("IdProduct", idProduct);
        command.Parameters.AddWithValue("IdWarehouse",idWarehouse);
        command.Parameters.AddWithValue("Amount", 0);
        command.Parameters.AddWithValue("CreatedAt", createdAt);
        await command.ExecuteNonQueryAsync();
    }
}
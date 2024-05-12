using WebApplication2.Dto;
using WebApplication2.Exceptions;
using WebApplication2.Repositories;

namespace WebApplication2.Services;

public interface IWarehouseService
{
    public Task<int> RegisterProductInWarehouseAsync(RegisterProductInWarehouseRequestDTO dto);
}

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    public WarehouseService(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }
    
    public async Task<int> RegisterProductInWarehouseAsync(RegisterProductInWarehouseRequestDTO dto)
    {
        // Example Flow:
        // check if product exists else throw NotFoundException
        var product_Exists = await _warehouseRepository.CheckProductExistsAsync(dto.IdProduct!.Value);
        if (!product_Exists)
        {
            throw new NotFoundException("No product found");
        }
        // check if warehouse exists else throw NotFoundException
        var warehouse_Exists = await _warehouseRepository.CheckWarehouseExistsAsync(dto.IdWarehouse!.Value);
        if (!warehouse_Exists)
        {
            throw new NotFoundException("No warehouse found");
        }
        // get order if exists else throw NotFoundException
        var order = await _warehouseRepository.GetOrderIfExistsAsync(dto.IdProduct!.Value, dto.Amount!.Value, dto.CreatedAt.Value);
        if (order == null)
        {
            throw new NotFoundException("No order was found");
        }
        const int idOrder = 1;
        // check if product is already in warehouse else throw ConflictException
        var productInWarehouse = await _warehouseRepository.CheckProductInWarehouseAsync(dto.IdProduct!.Value, idOrder);
        if (!productInWarehouse)
        {
            throw new ConflictException("Already in Warehouse");
        }
        
        var idProductWarehouse = await _warehouseRepository.RegisterProductInWarehouseAsync(
            idWarehouse: dto.IdWarehouse!.Value,
            idProduct: dto.IdProduct!.Value,
            idOrder: idOrder,
            createdAt: DateTime.UtcNow);

        if (!idProductWarehouse.HasValue)
            throw new Exception("Failed to register product in warehouse");

        return idProductWarehouse.Value;
    }
}
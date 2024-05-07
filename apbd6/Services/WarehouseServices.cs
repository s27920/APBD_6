using WebApplication2.Dto;
using WebApplication2.Exceptions;

namespace apbd6.Services;


public interface IWarehouseService
{
    public Task<int> FulfillOrderAsync(RegisterProductInWarehouseRequestDTO dto);
}

public class WarehouseServices : IWarehouseService
{
    private IWarehouseRepository _iWarehouseRepository;

    public WarehouseServices(IWarehouseRepository iWarehouseRepository)
    {
        _iWarehouseRepository = iWarehouseRepository;
    }

    public async Task<int> FulfillOrderAsync(RegisterProductInWarehouseRequestDTO dto)
    {
        const int idOrder = 1;
        if (dto.Ammount <= 0)
        {
            throw new ConflictException("Entered amount is incorrect");
        }
        if (!_iWarehouseRepository.CheckProductByIdAsync(dto.IdProduct).Result)
        {
            throw new NotFoundException("No such product exists");
        }
        if (!_iWarehouseRepository.CheckWarehouseByIdAsync(dto.IdWarehouse).Result)
        {
            throw new NotFoundException("No such warehouse exists");
        }
        if (!_iWarehouseRepository.CheckOrderCorrectnessAsync(dto.IdProduct, dto.Ammount).Result)
        {
            throw new NotFoundException("No such order exists");
        }

        if (_iWarehouseRepository.CheckFulfilled(idOrder).Result)
        {
            throw new ConflictException("Order already fulfilled");
        }

        int? result = await _iWarehouseRepository.RegisterProductAsync(dto.IdProduct.Value, dto.IdWarehouse.Value, dto.Ammount.Value);
        // await _iWarehouseRepository.ProcRegisterProductAsync(dto.IdProduct.Value, dto.IdWarehouse.Value, dto.Ammount.Value);
        if (result == null)
        {
            throw new ConflictException("Transaction failed product not registered");
        }
        return result.Value;
    }
}
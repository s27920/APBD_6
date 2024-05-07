using apbd6.Services;
using Microsoft.AspNetCore.Mvc;
using WebApplication2.Dto;
using WebApplication2.Exceptions;

namespace apbd6.Controllers;


[ApiController]
[Route("/api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    public async Task<IActionResult> RegisterProductInWarehouse([FromBody] RegisterProductInWarehouseRequestDTO dto)
    {
        try
        {
            int warehouseId = await _warehouseService.FulfillOrderAsync(dto);
            return Ok(warehouseId);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
    }
}
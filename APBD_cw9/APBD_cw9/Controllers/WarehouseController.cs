using APBD_cw9.Models;
using APBD_cw9.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_cw9.Controllers;

[ApiController]
[Route("api/[controller]")]

public class WarehouseController : ControllerBase
{
    private IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] ProductAtWarehouseInfo request)
    {
        try
        {
            var newId = await _dbService.AddProductAsync(request.ProductId, request.WarehouseId, request.Amount,
                request.CreatedAt);
            return Ok(new { Id = newId });
        }
        catch (ArgumentException ArgEx)
        {
            return BadRequest(ArgEx.Message);
        }
        catch (InvalidOperationException InOpEx)
        {
            return Conflict(InOpEx.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex);
        }
    }
}
using METS.Api.Data;
using METS.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace METS.Api.Controllers;

[ApiController]
[Route("api/locations")]
[Produces("application/json")]
public class LocationsController(MetsDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<StockLocationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var locations = await db.StockLocations
            .AsNoTracking()
            .OrderBy(l => l.Code)
            .Select(l => l.ToDto())
            .ToListAsync();
        return Ok(locations);
    }
}

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController(MetsDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<UserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? role = null)
    {
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(role) &&
            Enum.TryParse<Models.UserRole>(role, ignoreCase: true, out var parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        var users = await query
            .OrderBy(u => u.Name)
            .Select(u => u.ToDto())
            .ToListAsync();

        return Ok(users);
    }
}

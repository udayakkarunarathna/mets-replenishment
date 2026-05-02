using METS.Api.DTOs;
using METS.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace METS.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Produces("application/json")]
public class ReplenishmentRequestsController(IReplenishmentService service) : ControllerBase
{
    /// <summary>List requests with optional filtering and pagination.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<RequestSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] RequestFilterDto filter)
    {
        var result = await service.GetRequestsAsync(filter);
        return Ok(result);
    }

    /// <summary>Get a single request by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetRequestByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Poll validation status for a submitted request.</summary>
    [HttpGet("{id:int}/validation")]
    [ProducesResponseType<ValidationResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetValidation(int id)
    {
        var result = await service.GetValidationStatusAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new draft request.</summary>
    [HttpPost]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto dto)
    {
        var result = await service.CreateRequestAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Update a draft request.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRequestDto dto)
    {
        try
        {
            var result = await service.UpdateRequestAsync(id, dto);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Submit a draft for approval. Triggers async stock validation (202 Accepted).</summary>
    [HttpPost("{id:int}/submit")]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitRequestDto dto)
    {
        var (success, error, result) = await service.SubmitRequestAsync(id, dto);
        if (!success) return error!.Contains("not found") ? NotFound(new { error }) : BadRequest(new { error });
        return AcceptedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>Approve a submitted request.</summary>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveRequestDto dto)
    {
        var (success, error, result) = await service.ApproveRequestAsync(id, dto);
        if (!success) return error!.Contains("not found") ? NotFound(new { error }) : BadRequest(new { error });
        return Ok(result);
    }

    /// <summary>Reject a submitted request with a reason.</summary>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectRequestDto dto)
    {
        var (success, error, result) = await service.RejectRequestAsync(id, dto);
        if (!success) return error!.Contains("not found") ? NotFound(new { error }) : BadRequest(new { error });
        return Ok(result);
    }

    /// <summary>Mark an approved request as fulfilled with actual quantities.</summary>
    [HttpPost("{id:int}/fulfill")]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Fulfill(int id, [FromBody] FulfillRequestDto dto)
    {
        var (success, error, result) = await service.FulfillRequestAsync(id, dto);
        if (!success) return error!.Contains("not found") ? NotFound(new { error }) : BadRequest(new { error });
        return Ok(result);
    }
}

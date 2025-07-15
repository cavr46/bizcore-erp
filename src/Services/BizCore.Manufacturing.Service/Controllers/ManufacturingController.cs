using BizCore.Manufacturing.Domain.Entities;
using BizCore.Manufacturing.Grains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizCore.Manufacturing.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ManufacturingController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<ManufacturingController> _logger;

    public ManufacturingController(IGrainFactory grainFactory, ILogger<ManufacturingController> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    // BOM Endpoints
    [HttpPost("boms")]
    public async Task<ActionResult<BillOfMaterials>> CreateBom([FromBody] CreateBomRequest request)
    {
        try
        {
            var bomGrain = _grainFactory.GetGrain<IBomGrain>(Guid.NewGuid());
            var bom = await bomGrain.CreateBomAsync(request);
            return Ok(bom);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating BOM");
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("boms/{id}")]
    public async Task<ActionResult<BillOfMaterials>> GetBom(Guid id)
    {
        try
        {
            var bomGrain = _grainFactory.GetGrain<IBomGrain>(id);
            var bom = await bomGrain.GetBomAsync();
            return Ok(bom);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving BOM {BomId}", id);
            return NotFound(ex.Message);
        }
    }

    [HttpPost("boms/{id}/components")]
    public async Task<ActionResult> AddComponent(Guid id, [FromBody] AddComponentRequest request)
    {
        try
        {
            var bomGrain = _grainFactory.GetGrain<IBomGrain>(id);
            await bomGrain.AddComponentAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding component to BOM {BomId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("boms/{id}/components/{sequence}")]
    public async Task<ActionResult> UpdateComponent(Guid id, int sequence, [FromBody] UpdateComponentRequest request)
    {
        try
        {
            var bomGrain = _grainFactory.GetGrain<IBomGrain>(id);
            await bomGrain.UpdateComponentAsync(sequence, request.Quantity, request.ScrapFactor);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating component in BOM {BomId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("boms/{id}/components/{sequence}")]
    public async Task<ActionResult> RemoveComponent(Guid id, int sequence)
    {
        try
        {
            var bomGrain = _grainFactory.GetGrain<IBomGrain>(id);
            await bomGrain.RemoveComponentAsync(sequence);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing component from BOM {BomId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("boms/{id}/operations")]
    public async Task<ActionResult> AddOperation(Guid id, [FromBody] AddBomOperationRequest request)
    {
        try
        {
            var bomGrain = _grainFactory.GetGrain<IBomGrain>(id);
            await bomGrain.AddOperationAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding operation to BOM {BomId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("boms/{id}/activate")]
    public async Task<ActionResult> ActivateBom(Guid id)
    {
        try
        {
            var bomGrain = _grainFactory.GetGrain<IBomGrain>(id);
            var result = await bomGrain.ActivateAsync();
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating BOM {BomId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("boms/{id}/deactivate")]
    public async Task<ActionResult> DeactivateBom(Guid id)
    {
        try
        {
            var bomGrain = _grainFactory.GetGrain<IBomGrain>(id);
            await bomGrain.DeactivateAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating BOM {BomId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("boms/{id}/calculate-material-requirement")]
    public async Task<ActionResult<MaterialRequirement>> CalculateMaterialRequirement(Guid id, [FromBody] CalculateMaterialRequirementRequest request)
    {
        try
        {
            var bomGrain = _grainFactory.GetGrain<IBomGrain>(id);
            var requirement = await bomGrain.CalculateMaterialRequirementAsync(request.RequiredQuantity);
            return Ok(requirement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating material requirement for BOM {BomId}", id);
            return BadRequest(ex.Message);
        }
    }

    // Work Order Endpoints
    [HttpPost("work-orders")]
    public async Task<ActionResult<WorkOrder>> CreateWorkOrder([FromBody] CreateWorkOrderRequest request)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(Guid.NewGuid());
            var workOrder = await workOrderGrain.CreateWorkOrderAsync(request);
            return Ok(workOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating work order");
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("work-orders/{id}")]
    public async Task<ActionResult<WorkOrder>> GetWorkOrder(Guid id)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            var workOrder = await workOrderGrain.GetWorkOrderAsync();
            return Ok(workOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work order {WorkOrderId}", id);
            return NotFound(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/release")]
    public async Task<ActionResult> ReleaseWorkOrder(Guid id)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            var result = await workOrderGrain.ReleaseWorkOrderAsync();
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/start")]
    public async Task<ActionResult> StartWorkOrder(Guid id)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            var result = await workOrderGrain.StartWorkOrderAsync();
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/complete")]
    public async Task<ActionResult> CompleteWorkOrder(Guid id, [FromBody] CompleteWorkOrderRequest request)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            var result = await workOrderGrain.CompleteWorkOrderAsync(request.CompletedQuantity, request.ScrapQuantity);
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/close")]
    public async Task<ActionResult> CloseWorkOrder(Guid id)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            var result = await workOrderGrain.CloseWorkOrderAsync();
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/cancel")]
    public async Task<ActionResult> CancelWorkOrder(Guid id, [FromBody] CancelWorkOrderRequest request)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            var result = await workOrderGrain.CancelWorkOrderAsync(request.Reason);
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/hold")]
    public async Task<ActionResult> HoldWorkOrder(Guid id, [FromBody] HoldWorkOrderRequest request)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            await workOrderGrain.HoldWorkOrderAsync(request.Reason);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error holding work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/resume")]
    public async Task<ActionResult> ResumeWorkOrder(Guid id)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            await workOrderGrain.ResumeWorkOrderAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/operations")]
    public async Task<ActionResult> AddOperation(Guid id, [FromBody] AddOperationRequest request)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            await workOrderGrain.AddOperationAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding operation to work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/materials")]
    public async Task<ActionResult> AddMaterial(Guid id, [FromBody] AddMaterialRequest request)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            await workOrderGrain.AddMaterialAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding material to work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/materials/{materialId}/issue")]
    public async Task<ActionResult> IssueMaterial(Guid id, Guid materialId, [FromBody] IssueMaterialRequest request)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            await workOrderGrain.IssueMaterialAsync(materialId, request.Quantity);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error issuing material to work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/time-entries")]
    public async Task<ActionResult> AddTimeEntry(Guid id, [FromBody] AddTimeEntryRequest request)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            await workOrderGrain.AddTimeEntryAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding time entry to work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("work-orders/{id}/quality-checks")]
    public async Task<ActionResult> AddQualityCheck(Guid id, [FromBody] AddQualityCheckRequest request)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            await workOrderGrain.AddQualityCheckAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding quality check to work order {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("work-orders/{id}/statistics")]
    public async Task<ActionResult<WorkOrderStatistics>> GetWorkOrderStatistics(Guid id)
    {
        try
        {
            var workOrderGrain = _grainFactory.GetGrain<IWorkOrderGrain>(id);
            var statistics = await workOrderGrain.GetStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work order statistics {WorkOrderId}", id);
            return BadRequest(ex.Message);
        }
    }
}

// Additional Request DTOs
public record UpdateComponentRequest(decimal Quantity, decimal ScrapFactor);
public record CalculateMaterialRequirementRequest(decimal RequiredQuantity);
public record CompleteWorkOrderRequest(decimal CompletedQuantity, decimal? ScrapQuantity = null);
public record CancelWorkOrderRequest(string Reason);
public record HoldWorkOrderRequest(string Reason);
public record IssueMaterialRequest(decimal Quantity);
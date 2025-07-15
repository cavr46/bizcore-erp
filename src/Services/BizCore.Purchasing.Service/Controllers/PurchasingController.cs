using BizCore.Purchasing.Domain.Entities;
using BizCore.Purchasing.Grains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizCore.Purchasing.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchasingController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<PurchasingController> _logger;

    public PurchasingController(IGrainFactory grainFactory, ILogger<PurchasingController> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    // Supplier Endpoints
    [HttpPost("suppliers")]
    public async Task<ActionResult<Supplier>> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(Guid.NewGuid());
            var supplier = await supplierGrain.CreateSupplierAsync(request);
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier");
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("suppliers/{id}")]
    public async Task<ActionResult<Supplier>> GetSupplier(Guid id)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            var supplier = await supplierGrain.GetSupplierAsync();
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier {SupplierId}", id);
            return NotFound(ex.Message);
        }
    }

    [HttpPut("suppliers/{id}/basic-info")]
    public async Task<ActionResult> UpdateSupplierBasicInfo(Guid id, [FromBody] UpdateSupplierBasicInfoRequest request)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            await supplierGrain.UpdateBasicInfoAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier basic info {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("suppliers/{id}/credit-limit")]
    public async Task<ActionResult> SetCreditLimit(Guid id, [FromBody] SetCreditLimitRequest request)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            await supplierGrain.SetCreditLimitAsync(request.CreditLimit);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting credit limit for supplier {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("suppliers/{id}/payment-terms")]
    public async Task<ActionResult> SetPaymentTerms(Guid id, [FromBody] SetPaymentTermsRequest request)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            await supplierGrain.SetPaymentTermsAsync(request.Days);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting payment terms for supplier {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("suppliers/{id}/rating")]
    public async Task<ActionResult> UpdateRating(Guid id, [FromBody] UpdateRatingRequest request)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            await supplierGrain.UpdateRatingAsync(request.Rating);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rating for supplier {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("suppliers/{id}/addresses")]
    public async Task<ActionResult> AddAddress(Guid id, [FromBody] AddSupplierAddressRequest request)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            await supplierGrain.AddAddressAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding address to supplier {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("suppliers/{id}/contacts")]
    public async Task<ActionResult> AddContact(Guid id, [FromBody] AddSupplierContactRequest request)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            await supplierGrain.AddContactAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding contact to supplier {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("suppliers/{id}/certifications")]
    public async Task<ActionResult> AddCertification(Guid id, [FromBody] AddSupplierCertificationRequest request)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            await supplierGrain.AddCertificationAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding certification to supplier {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("suppliers/{id}/performance-metrics")]
    public async Task<ActionResult> UpdatePerformanceMetric(Guid id, [FromBody] UpdatePerformanceMetricRequest request)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            await supplierGrain.UpdatePerformanceMetricAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance metric for supplier {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("suppliers/{id}/activate")]
    public async Task<ActionResult> ActivateSupplier(Guid id)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            await supplierGrain.ActivateAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating supplier {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("suppliers/{id}/deactivate")]
    public async Task<ActionResult> DeactivateSupplier(Guid id)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            await supplierGrain.DeactivateAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating supplier {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("suppliers/{id}/performance-report")]
    public async Task<ActionResult<SupplierPerformanceReport>> GetPerformanceReport(Guid id)
    {
        try
        {
            var supplierGrain = _grainFactory.GetGrain<ISupplierGrain>(id);
            var report = await supplierGrain.GetPerformanceReportAsync();
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance report for supplier {SupplierId}", id);
            return BadRequest(ex.Message);
        }
    }

    // Purchase Order Endpoints
    [HttpPost("purchase-orders")]
    public async Task<ActionResult<PurchaseOrder>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(Guid.NewGuid());
            var purchaseOrder = await purchaseOrderGrain.CreatePurchaseOrderAsync(request);
            return Ok(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase order");
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("purchase-orders/{id}")]
    public async Task<ActionResult<PurchaseOrder>> GetPurchaseOrder(Guid id)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            var purchaseOrder = await purchaseOrderGrain.GetPurchaseOrderAsync();
            return Ok(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase order {OrderId}", id);
            return NotFound(ex.Message);
        }
    }

    [HttpPost("purchase-orders/{id}/lines")]
    public async Task<ActionResult> AddLine(Guid id, [FromBody] AddPurchaseOrderLineRequest request)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            await purchaseOrderGrain.AddLineAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding line to purchase order {OrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("purchase-orders/{id}/lines/{lineNumber}")]
    public async Task<ActionResult> UpdateLine(Guid id, int lineNumber, [FromBody] UpdatePurchaseOrderLineRequest request)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            await purchaseOrderGrain.UpdateLineAsync(lineNumber, request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating line {LineNumber} in purchase order {OrderId}", lineNumber, id);
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("purchase-orders/{id}/lines/{lineNumber}")]
    public async Task<ActionResult> RemoveLine(Guid id, int lineNumber)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            await purchaseOrderGrain.RemoveLineAsync(lineNumber);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing line {LineNumber} from purchase order {OrderId}", lineNumber, id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("purchase-orders/{id}/discount")]
    public async Task<ActionResult> SetDiscount(Guid id, [FromBody] SetDiscountRequest request)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            await purchaseOrderGrain.SetDiscountAsync(request.DiscountPercentage);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting discount for purchase order {OrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("purchase-orders/{id}/shipping")]
    public async Task<ActionResult> SetShipping(Guid id, [FromBody] SetShippingRequest request)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            await purchaseOrderGrain.SetShippingAsync(request.ShippingAmount, request.Currency);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting shipping for purchase order {OrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("purchase-orders/{id}/submit")]
    public async Task<ActionResult> SubmitPurchaseOrder(Guid id)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            var result = await purchaseOrderGrain.SubmitAsync();
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting purchase order {OrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("purchase-orders/{id}/approve")]
    public async Task<ActionResult> ApprovePurchaseOrder(Guid id, [FromBody] ApproveOrderRequest request)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            var result = await purchaseOrderGrain.ApproveAsync(request.ApprovedBy);
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving purchase order {OrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("purchase-orders/{id}/send")]
    public async Task<ActionResult> SendPurchaseOrder(Guid id)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            var result = await purchaseOrderGrain.SendAsync();
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending purchase order {OrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("purchase-orders/{id}/acknowledge")]
    public async Task<ActionResult> AcknowledgePurchaseOrder(Guid id)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            var result = await purchaseOrderGrain.AcknowledgeAsync();
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging purchase order {OrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("purchase-orders/{id}/cancel")]
    public async Task<ActionResult> CancelPurchaseOrder(Guid id, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            var result = await purchaseOrderGrain.CancelAsync(request.CancelledBy, request.Reason);
            
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling purchase order {OrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("purchase-orders/{id}/receipts")]
    public async Task<ActionResult<PurchaseOrderReceipt>> CreateReceipt(Guid id, [FromBody] CreateReceiptRequest request)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            var receipt = await purchaseOrderGrain.CreateReceiptAsync(request);
            return Ok(receipt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating receipt for purchase order {OrderId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("purchase-orders/{id}/summary")]
    public async Task<ActionResult<PurchaseOrderSummary>> GetPurchaseOrderSummary(Guid id)
    {
        try
        {
            var purchaseOrderGrain = _grainFactory.GetGrain<IPurchaseOrderGrain>(id);
            var summary = await purchaseOrderGrain.GetSummaryAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase order summary {OrderId}", id);
            return BadRequest(ex.Message);
        }
    }
}

// Additional Request DTOs
public record SetCreditLimitRequest(decimal CreditLimit);
public record SetPaymentTermsRequest(int Days);
public record UpdateRatingRequest(SupplierRating Rating);
public record SetDiscountRequest(decimal DiscountPercentage);
public record SetShippingRequest(decimal ShippingAmount, string Currency);
public record ApproveOrderRequest(Guid ApprovedBy);
public record CancelOrderRequest(Guid CancelledBy, string Reason);
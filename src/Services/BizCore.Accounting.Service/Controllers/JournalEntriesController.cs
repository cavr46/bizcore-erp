using BizCore.Accounting.Grains;
using BizCore.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace BizCore.Accounting.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JournalEntriesController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUserService _currentUserService;

    public JournalEntriesController(IGrainFactory grainFactory, ICurrentUserService currentUserService)
    {
        _grainFactory = grainFactory;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateJournalEntry([FromBody] CreateJournalEntryDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var managerGrain = _grainFactory.GetGrain<IAccountingManagerGrain>(_currentUserService.TenantId.Value);
        var entryNumber = await managerGrain.GenerateJournalEntryNumberAsync();

        var request = new CreateJournalEntryRequest(
            _currentUserService.TenantId.Value,
            entryNumber,
            dto.Date,
            dto.Description,
            dto.Type);

        var entryId = Guid.NewGuid();
        var entryGrain = _grainFactory.GetGrain<IJournalEntryGrain>($"{_currentUserService.TenantId}_{entryId}");
        var entry = await entryGrain.CreateEntryAsync(request);

        return Ok(MapToDto(entry));
    }

    [HttpGet("{entryId}")]
    public async Task<IActionResult> GetJournalEntry(Guid entryId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var entryGrain = _grainFactory.GetGrain<IJournalEntryGrain>($"{_currentUserService.TenantId}_{entryId}");
        var entry = await entryGrain.GetEntryAsync();

        if (entry == null)
            return NotFound();

        return Ok(MapToDto(entry));
    }

    [HttpPost("{entryId}/lines")]
    public async Task<IActionResult> AddJournalLine(Guid entryId, [FromBody] AddJournalLineDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new AddJournalLineRequest(
            dto.AccountId,
            dto.Description,
            dto.DebitAmount != null ? new Domain.Common.Money(dto.DebitAmount.Value, dto.Currency) : null,
            dto.CreditAmount != null ? new Domain.Common.Money(dto.CreditAmount.Value, dto.Currency) : null,
            dto.CostCenterId,
            dto.ProjectId);

        var entryGrain = _grainFactory.GetGrain<IJournalEntryGrain>($"{_currentUserService.TenantId}_{entryId}");
        
        try
        {
            await entryGrain.AddLineAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{entryId}/lines/{lineNumber}")]
    public async Task<IActionResult> RemoveJournalLine(Guid entryId, int lineNumber)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var entryGrain = _grainFactory.GetGrain<IJournalEntryGrain>($"{_currentUserService.TenantId}_{entryId}");
        
        try
        {
            await entryGrain.RemoveLineAsync(lineNumber);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{entryId}/validate")]
    public async Task<IActionResult> ValidateEntry(Guid entryId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var entryGrain = _grainFactory.GetGrain<IJournalEntryGrain>($"{_currentUserService.TenantId}_{entryId}");
        var result = await entryGrain.ValidateAsync();

        if (result.IsSuccess)
            return Ok(new { IsValid = true });
        else
            return BadRequest(new { IsValid = false, Error = result.Error });
    }

    [HttpPost("{entryId}/submit")]
    public async Task<IActionResult> SubmitEntry(Guid entryId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var entryGrain = _grainFactory.GetGrain<IJournalEntryGrain>($"{_currentUserService.TenantId}_{entryId}");
        var result = await entryGrain.SubmitAsync();

        if (result.IsSuccess)
            return Ok();
        else
            return BadRequest(result.Error);
    }

    [HttpPost("{entryId}/approve")]
    public async Task<IActionResult> ApproveEntry(Guid entryId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var entryGrain = _grainFactory.GetGrain<IJournalEntryGrain>($"{_currentUserService.TenantId}_{entryId}");
        var result = await entryGrain.ApproveAsync(_currentUserService.UserId ?? "System");

        if (result.IsSuccess)
            return Ok();
        else
            return BadRequest(result.Error);
    }

    [HttpPost("{entryId}/post")]
    public async Task<IActionResult> PostEntry(Guid entryId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var entryGrain = _grainFactory.GetGrain<IJournalEntryGrain>($"{_currentUserService.TenantId}_{entryId}");
        var result = await entryGrain.PostAsync(_currentUserService.UserId ?? "System");

        if (result.IsSuccess)
            return Ok();
        else
            return BadRequest(result.Error);
    }

    [HttpPost("{entryId}/reverse")]
    public async Task<IActionResult> ReverseEntry(Guid entryId, [FromBody] CreateReversalDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var managerGrain = _grainFactory.GetGrain<IAccountingManagerGrain>(_currentUserService.TenantId.Value);
        var reversalNumber = await managerGrain.GenerateJournalEntryNumberAsync();

        var request = new CreateReversalRequest(reversalNumber, dto.Date, dto.Reason);
        var entryGrain = _grainFactory.GetGrain<IJournalEntryGrain>($"{_currentUserService.TenantId}_{entryId}");
        var result = await entryGrain.CreateReversalAsync(request);

        if (result.IsSuccess)
        {
            return Ok(MapToDto(result.Value!));
        }
        else
        {
            return BadRequest(result.Error);
        }
    }

    [HttpPost("{entryId}/attachments")]
    public async Task<IActionResult> AddAttachment(Guid entryId, [FromBody] AddAttachmentDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new AddAttachmentRequest(dto.FileName, dto.FileUrl, _currentUserService.UserId ?? "System");
        var entryGrain = _grainFactory.GetGrain<IJournalEntryGrain>($"{_currentUserService.TenantId}_{entryId}");
        
        try
        {
            await entryGrain.AddAttachmentAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static JournalEntryDto MapToDto(Domain.Entities.JournalEntry entry)
    {
        return new JournalEntryDto(
            entry.Id,
            entry.EntryNumber,
            entry.Date,
            entry.Description,
            entry.Reference,
            entry.Status.Name,
            entry.Type.Name,
            entry.ApprovedBy,
            entry.ApprovedAt,
            entry.PostedBy,
            entry.PostedAt,
            entry.Lines.Select(MapLineToDto).ToList());
    }

    private static JournalEntryLineDto MapLineToDto(Domain.Entities.JournalEntryLine line)
    {
        return new JournalEntryLineDto(
            line.LineNumber,
            line.AccountId,
            line.Description,
            line.DebitAmount?.Amount,
            line.CreditAmount?.Amount,
            line.CostCenterId,
            line.ProjectId);
    }
}

public record CreateJournalEntryDto(
    DateTime Date,
    string Description,
    Domain.Entities.EntryType Type);

public record AddJournalLineDto(
    Guid AccountId,
    string Description,
    decimal? DebitAmount,
    decimal? CreditAmount,
    string Currency,
    Guid? CostCenterId,
    Guid? ProjectId);

public record CreateReversalDto(
    DateTime Date,
    string Reason);

public record AddAttachmentDto(
    string FileName,
    string FileUrl);

public record JournalEntryDto(
    Guid Id,
    string EntryNumber,
    DateTime Date,
    string Description,
    string? Reference,
    string Status,
    string Type,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    string? PostedBy,
    DateTime? PostedAt,
    List<JournalEntryLineDto> Lines);

public record JournalEntryLineDto(
    int LineNumber,
    Guid AccountId,
    string Description,
    decimal? DebitAmount,
    decimal? CreditAmount,
    Guid? CostCenterId,
    Guid? ProjectId);
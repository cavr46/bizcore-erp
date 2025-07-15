using BizCore.Accounting.Grains;
using BizCore.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace BizCore.Accounting.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUserService _currentUserService;

    public AccountsController(IGrainFactory grainFactory, ICurrentUserService currentUserService)
    {
        _grainFactory = grainFactory;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var managerGrain = _grainFactory.GetGrain<IAccountingManagerGrain>(_currentUserService.TenantId.Value);
        var accountCode = await managerGrain.GenerateAccountCodeAsync(dto.Type.Code);

        var request = new CreateAccountRequest(
            _currentUserService.TenantId.Value,
            accountCode,
            dto.Name,
            dto.Type,
            dto.Currency,
            dto.ParentAccountId);

        var accountGrain = _grainFactory.GetGrain<IAccountGrain>($"{_currentUserService.TenantId}_{Guid.NewGuid()}");
        var account = await accountGrain.CreateAccountAsync(request);

        return Ok(MapToDto(account));
    }

    [HttpGet("{accountId}")]
    public async Task<IActionResult> GetAccount(Guid accountId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var accountGrain = _grainFactory.GetGrain<IAccountGrain>($"{_currentUserService.TenantId}_{accountId}");
        var account = await accountGrain.GetAccountAsync();

        if (account == null)
            return NotFound();

        return Ok(MapToDto(account));
    }

    [HttpPut("{accountId}")]
    public async Task<IActionResult> UpdateAccount(Guid accountId, [FromBody] UpdateAccountDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new UpdateAccountRequest(dto.Name, dto.Description);
        var accountGrain = _grainFactory.GetGrain<IAccountGrain>($"{_currentUserService.TenantId}_{accountId}");
        
        try
        {
            var account = await accountGrain.UpdateAccountAsync(request);
            return Ok(MapToDto(account));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{accountId}/activate")]
    public async Task<IActionResult> ActivateAccount(Guid accountId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var accountGrain = _grainFactory.GetGrain<IAccountGrain>($"{_currentUserService.TenantId}_{accountId}");
        
        try
        {
            await accountGrain.ActivateAccountAsync();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{accountId}/deactivate")]
    public async Task<IActionResult> DeactivateAccount(Guid accountId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var accountGrain = _grainFactory.GetGrain<IAccountGrain>($"{_currentUserService.TenantId}_{accountId}");
        
        try
        {
            await accountGrain.DeactivateAccountAsync();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{accountId}/balance")]
    public async Task<IActionResult> GetCurrentBalance(Guid accountId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var accountGrain = _grainFactory.GetGrain<IAccountGrain>($"{_currentUserService.TenantId}_{accountId}");
        
        try
        {
            var balance = await accountGrain.GetCurrentBalanceAsync();
            return Ok(new { Balance = balance });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{accountId}/balances/{year}")]
    public async Task<IActionResult> GetYearBalances(Guid accountId, int year)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var accountGrain = _grainFactory.GetGrain<IAccountGrain>($"{_currentUserService.TenantId}_{accountId}");
        
        try
        {
            var balances = await accountGrain.GetBalancesAsync(year);
            return Ok(balances.Select(MapBalanceToDto));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    private static AccountDto MapToDto(Domain.Entities.Account account)
    {
        return new AccountDto(
            account.Id,
            account.Code,
            account.Name,
            account.Description,
            account.Type.Name,
            account.Currency,
            account.ParentAccountId,
            account.Level,
            account.IsActive,
            account.AllowManualEntry,
            account.RequiresCostCenter,
            account.RequiresProject);
    }

    private static AccountBalanceDto MapBalanceToDto(Domain.Entities.AccountBalance balance)
    {
        return new AccountBalanceDto(
            balance.Year,
            balance.Month,
            balance.OpeningBalance.Amount,
            balance.Debits.Amount,
            balance.Credits.Amount,
            balance.ClosingBalance.Amount,
            balance.IsClosed);
    }
}

public record CreateAccountDto(
    string Name,
    string? Description,
    Domain.Entities.AccountType Type,
    string Currency,
    Guid? ParentAccountId);

public record UpdateAccountDto(
    string Name,
    string? Description);

public record AccountDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    string Currency,
    Guid? ParentAccountId,
    int Level,
    bool IsActive,
    bool AllowManualEntry,
    bool RequiresCostCenter,
    bool RequiresProject);

public record AccountBalanceDto(
    int Year,
    int Month,
    decimal OpeningBalance,
    decimal Debits,
    decimal Credits,
    decimal ClosingBalance,
    bool IsClosed);
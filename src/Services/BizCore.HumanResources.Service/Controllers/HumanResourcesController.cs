using BizCore.HumanResources.Domain.Entities;
using BizCore.HumanResources.Grains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizCore.HumanResources.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HumanResourcesController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<HumanResourcesController> _logger;

    public HumanResourcesController(IGrainFactory grainFactory, ILogger<HumanResourcesController> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    // Employee Management
    [HttpPost("employees")]
    public async Task<ActionResult<Employee>> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(Guid.NewGuid());
            var employee = await employeeGrain.CreateEmployeeAsync(request);
            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("employees/{id}")]
    public async Task<ActionResult<Employee>> GetEmployee(Guid id)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            var employee = await employeeGrain.GetEmployeeAsync();
            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee {EmployeeId}", id);
            return NotFound(ex.Message);
        }
    }

    [HttpPut("employees/{id}/personal-info")]
    public async Task<ActionResult> UpdatePersonalInfo(Guid id, [FromBody] UpdatePersonalInfoRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.UpdatePersonalInfoAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating personal info for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("employees/{id}/identification")]
    public async Task<ActionResult> UpdateIdentification(Guid id, [FromBody] UpdateIdentificationRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.UpdateIdentificationAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating identification for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("employees/{id}/employment")]
    public async Task<ActionResult> UpdateEmployment(Guid id, [FromBody] UpdateEmploymentRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.UpdateEmploymentAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employment for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("employees/{id}/compensation")]
    public async Task<ActionResult> UpdateCompensation(Guid id, [FromBody] UpdateCompensationRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.UpdateCompensationAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating compensation for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("employees/{id}/addresses")]
    public async Task<ActionResult> AddAddress(Guid id, [FromBody] AddAddressRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.AddAddressAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding address for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("employees/{id}/emergency-contacts")]
    public async Task<ActionResult> AddEmergencyContact(Guid id, [FromBody] AddEmergencyContactRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.AddEmergencyContactAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding emergency contact for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    // Benefits Management
    [HttpPost("employees/{id}/benefits/enroll")]
    public async Task<ActionResult> EnrollInBenefit(Guid id, [FromBody] EnrollBenefitRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.EnrollInBenefitAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling employee {EmployeeId} in benefit", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("employees/{id}/benefits/unenroll")]
    public async Task<ActionResult> UnenrollFromBenefit(Guid id, [FromBody] UnenrollBenefitRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.UnenrollFromBenefitAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unenrolling employee {EmployeeId} from benefit", id);
            return BadRequest(ex.Message);
        }
    }

    // Skills Management
    [HttpPost("employees/{id}/skills")]
    public async Task<ActionResult> AddSkill(Guid id, [FromBody] AddSkillRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.AddSkillAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding skill for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("employees/{id}/skills/{skillName}")]
    public async Task<ActionResult> UpdateSkillLevel(Guid id, string skillName, [FromBody] UpdateSkillLevelRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.UpdateSkillLevelAsync(new UpdateSkillLevelRequest(skillName, request.Level));
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating skill level for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    // Document Management
    [HttpPost("employees/{id}/documents")]
    public async Task<ActionResult> AddDocument(Guid id, [FromBody] AddDocumentRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.AddDocumentAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    // Time Off Management
    [HttpPost("employees/{id}/time-off/request")]
    public async Task<ActionResult> RequestTimeOff(Guid id, [FromBody] RequestTimeOffRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.RequestTimeOffAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting time off for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("employees/{id}/time-off/approve")]
    public async Task<ActionResult> ApproveTimeOff(Guid id, [FromBody] ApproveTimeOffRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.ApproveTimeOffAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving time off for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("employees/{id}/time-off/history")]
    public async Task<ActionResult<List<EmployeeTimeOff>>> GetTimeOffHistory(Guid id)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            var history = await employeeGrain.GetTimeOffHistoryAsync();
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving time off history for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("employees/{id}/vacation-days/available")]
    public async Task<ActionResult<decimal>> GetAvailableVacationDays(Guid id)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            var availableDays = await employeeGrain.GetAvailableVacationDaysAsync();
            return Ok(availableDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available vacation days for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("employees/{id}/vacation-days/used")]
    public async Task<ActionResult<decimal>> GetUsedVacationDays(Guid id, [FromQuery] int year = 0)
    {
        try
        {
            var targetYear = year == 0 ? DateTime.Now.Year : year;
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            var usedDays = await employeeGrain.GetUsedVacationDaysAsync(targetYear);
            return Ok(usedDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving used vacation days for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    // Performance Management
    [HttpPost("employees/{id}/performance-review")]
    public async Task<ActionResult> RecordPerformanceReview(Guid id, [FromBody] RecordPerformanceReviewRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.RecordPerformanceReviewAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording performance review for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    // Career Management
    [HttpPost("employees/{id}/promote")]
    public async Task<ActionResult> PromoteEmployee(Guid id, [FromBody] PromoteRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.PromoteAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("employees/{id}/transfer")]
    public async Task<ActionResult> TransferEmployee(Guid id, [FromBody] TransferRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.TransferAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("employees/{id}/terminate")]
    public async Task<ActionResult> TerminateEmployee(Guid id, [FromBody] TerminateRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.TerminateAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("employees/{id}/suspend")]
    public async Task<ActionResult> SuspendEmployee(Guid id, [FromBody] SuspendRequest request)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.SuspendAsync(request);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("employees/{id}/reactivate")]
    public async Task<ActionResult> ReactivateEmployee(Guid id)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            await employeeGrain.ReactivateAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    // Analytics and Reporting
    [HttpGet("employees/{id}/statistics")]
    public async Task<ActionResult<EmployeeStatistics>> GetEmployeeStatistics(Guid id)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            var statistics = await employeeGrain.GetStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }

    // Payroll Integration Endpoints
    [HttpGet("employees/{id}/payroll-info")]
    public async Task<ActionResult<PayrollInfo>> GetPayrollInfo(Guid id)
    {
        try
        {
            var employeeGrain = _grainFactory.GetGrain<IEmployeeGrain>(id);
            var employee = await employeeGrain.GetEmployeeAsync();
            
            var payrollInfo = new PayrollInfo
            {
                EmployeeId = employee.Id,
                EmployeeNumber = employee.EmployeeNumber,
                FullName = employee.FullName,
                BaseSalary = employee.BaseSalary,
                HourlyRate = employee.HourlyRate,
                PayrollFrequency = employee.PayrollFrequency,
                TaxId = employee.TaxId,
                SocialSecurityNumber = employee.SocialSecurityNumber,
                ActiveBenefits = employee.Benefits.Where(b => b.IsActive).ToList(),
                YtdEarnings = 0, // Would be calculated from payroll system
                YtdDeductions = 0 // Would be calculated from payroll system
            };

            return Ok(payrollInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll info for employee {EmployeeId}", id);
            return BadRequest(ex.Message);
        }
    }
}

// Additional DTOs for specialized endpoints
public record PayrollInfo
{
    public Guid EmployeeId { get; init; }
    public string EmployeeNumber { get; init; }
    public string FullName { get; init; }
    public Money BaseSalary { get; init; }
    public Money? HourlyRate { get; init; }
    public PayrollFrequency PayrollFrequency { get; init; }
    public string? TaxId { get; init; }
    public string? SocialSecurityNumber { get; init; }
    public List<EmployeeBenefit> ActiveBenefits { get; init; } = new();
    public decimal YtdEarnings { get; init; }
    public decimal YtdDeductions { get; init; }
}
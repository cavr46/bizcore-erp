using BizCore.HumanResources.Domain.Entities;
using BizCore.Orleans.Core;
using Microsoft.Extensions.Logging;

namespace BizCore.HumanResources.Grains;

public interface IEmployeeGrain : IGrainWithGuidKey
{
    Task<Employee> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task<Employee> GetEmployeeAsync();
    Task UpdatePersonalInfoAsync(UpdatePersonalInfoRequest request);
    Task UpdateIdentificationAsync(UpdateIdentificationRequest request);
    Task UpdateEmploymentAsync(UpdateEmploymentRequest request);
    Task UpdateCompensationAsync(UpdateCompensationRequest request);
    Task AddAddressAsync(AddAddressRequest request);
    Task AddEmergencyContactAsync(AddEmergencyContactRequest request);
    Task EnrollInBenefitAsync(EnrollBenefitRequest request);
    Task UnenrollFromBenefitAsync(UnenrollBenefitRequest request);
    Task AddSkillAsync(AddSkillRequest request);
    Task UpdateSkillLevelAsync(UpdateSkillLevelRequest request);
    Task AddDocumentAsync(AddDocumentRequest request);
    Task RequestTimeOffAsync(RequestTimeOffRequest request);
    Task ApproveTimeOffAsync(ApproveTimeOffRequest request);
    Task RecordPerformanceReviewAsync(RecordPerformanceReviewRequest request);
    Task PromoteAsync(PromoteRequest request);
    Task TransferAsync(TransferRequest request);
    Task TerminateAsync(TerminateRequest request);
    Task SuspendAsync(SuspendRequest request);
    Task ReactivateAsync();
    Task<EmployeeStatistics> GetStatisticsAsync();
    Task<List<EmployeeTimeOff>> GetTimeOffHistoryAsync();
    Task<decimal> GetAvailableVacationDaysAsync();
    Task<decimal> GetUsedVacationDaysAsync(int year);
}

public class EmployeeGrain : TenantGrainBase<EmployeeState>, IEmployeeGrain
{
    private readonly ILogger<EmployeeGrain> _logger;

    public EmployeeGrain(ILogger<EmployeeGrain> logger)
    {
        _logger = logger;
    }

    public async Task<Employee> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        if (_state.State.Employee != null)
            throw new InvalidOperationException("Employee already exists");

        var baseSalary = new Money(request.BaseSalary, request.Currency);
        var employee = new Employee(
            GetTenantId(),
            request.EmployeeNumber,
            request.FirstName,
            request.LastName,
            request.Email,
            request.DateOfBirth,
            request.Gender,
            request.HireDate,
            request.EmploymentType,
            baseSalary,
            request.Currency);

        if (!string.IsNullOrEmpty(request.MiddleName))
            employee.UpdatePersonalInfo(request.FirstName, request.LastName, request.MiddleName, request.Email, request.Phone, request.Mobile, request.MaritalStatus);

        if (request.DepartmentId.HasValue)
            employee.UpdateEmployment(request.DepartmentId, request.PositionId, request.ManagerId, request.WorkLocation, request.EmploymentType);

        _state.State.Employee = employee;
        await SaveStateAsync();

        _logger.LogInformation("Employee {EmployeeNumber} created for tenant {TenantId}", 
            request.EmployeeNumber, GetTenantId());

        return employee;
    }

    public Task<Employee> GetEmployeeAsync()
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        return Task.FromResult(_state.State.Employee);
    }

    public async Task UpdatePersonalInfoAsync(UpdatePersonalInfoRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.UpdatePersonalInfo(
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.Email,
            request.Phone,
            request.Mobile,
            request.MaritalStatus);

        await SaveStateAsync();

        _logger.LogInformation("Personal info updated for employee {EmployeeNumber} for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, GetTenantId());
    }

    public async Task UpdateIdentificationAsync(UpdateIdentificationRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.UpdateIdentification(
            request.NationalId,
            request.TaxId,
            request.SocialSecurityNumber);

        await SaveStateAsync();

        _logger.LogInformation("Identification updated for employee {EmployeeNumber} for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, GetTenantId());
    }

    public async Task UpdateEmploymentAsync(UpdateEmploymentRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.UpdateEmployment(
            request.DepartmentId,
            request.PositionId,
            request.ManagerId,
            request.WorkLocation,
            request.EmploymentType);

        await SaveStateAsync();

        _logger.LogInformation("Employment updated for employee {EmployeeNumber} for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, GetTenantId());
    }

    public async Task UpdateCompensationAsync(UpdateCompensationRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        var baseSalary = new Money(request.BaseSalary, request.Currency);
        var hourlyRate = request.HourlyRate.HasValue ? new Money(request.HourlyRate.Value, request.Currency) : null;

        _state.State.Employee.UpdateCompensation(baseSalary, hourlyRate, request.PayrollFrequency);

        await SaveStateAsync();

        _logger.LogInformation("Compensation updated for employee {EmployeeNumber} for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, GetTenantId());
    }

    public async Task AddAddressAsync(AddAddressRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.AddAddress(
            request.Type,
            request.Street,
            request.City,
            request.State,
            request.PostalCode,
            request.Country);

        await SaveStateAsync();

        _logger.LogInformation("Address added for employee {EmployeeNumber} for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, GetTenantId());
    }

    public async Task AddEmergencyContactAsync(AddEmergencyContactRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.AddEmergencyContact(
            request.Name,
            request.Relationship,
            request.Phone,
            request.Email);

        await SaveStateAsync();

        _logger.LogInformation("Emergency contact added for employee {EmployeeNumber} for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, GetTenantId());
    }

    public async Task EnrollInBenefitAsync(EnrollBenefitRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.EnrollInBenefit(
            request.BenefitId,
            request.EnrollmentDate,
            request.EmployeeContribution);

        await SaveStateAsync();

        _logger.LogInformation("Employee {EmployeeNumber} enrolled in benefit {BenefitId} for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, request.BenefitId, GetTenantId());
    }

    public async Task UnenrollFromBenefitAsync(UnenrollBenefitRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.UnenrollFromBenefit(
            request.BenefitId,
            request.UnenrollmentDate);

        await SaveStateAsync();

        _logger.LogInformation("Employee {EmployeeNumber} unenrolled from benefit {BenefitId} for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, request.BenefitId, GetTenantId());
    }

    public async Task AddSkillAsync(AddSkillRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.AddSkill(
            request.SkillName,
            request.Level,
            request.CertificationDate,
            request.ExpiryDate);

        await SaveStateAsync();

        _logger.LogInformation("Skill {SkillName} added for employee {EmployeeNumber} for tenant {TenantId}", 
            request.SkillName, _state.State.Employee.EmployeeNumber, GetTenantId());
    }

    public async Task UpdateSkillLevelAsync(UpdateSkillLevelRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.UpdateSkillLevel(request.SkillName, request.Level);

        await SaveStateAsync();

        _logger.LogInformation("Skill level updated for {SkillName} for employee {EmployeeNumber} for tenant {TenantId}", 
            request.SkillName, _state.State.Employee.EmployeeNumber, GetTenantId());
    }

    public async Task AddDocumentAsync(AddDocumentRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.AddDocument(
            request.DocumentType,
            request.FileName,
            request.FileUrl,
            request.ExpiryDate);

        await SaveStateAsync();

        _logger.LogInformation("Document {DocumentType} added for employee {EmployeeNumber} for tenant {TenantId}", 
            request.DocumentType, _state.State.Employee.EmployeeNumber, GetTenantId());
    }

    public async Task RequestTimeOffAsync(RequestTimeOffRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.RequestTimeOff(
            request.Type,
            request.StartDate,
            request.EndDate,
            request.Days,
            request.Reason);

        await SaveStateAsync();

        _logger.LogInformation("Time off requested for employee {EmployeeNumber} for tenant {TenantId}: {Type} from {StartDate} to {EndDate}", 
            _state.State.Employee.EmployeeNumber, GetTenantId(), request.Type, request.StartDate, request.EndDate);
    }

    public async Task ApproveTimeOffAsync(ApproveTimeOffRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.ApproveTimeOff(request.TimeOffId, request.ApprovedBy);

        await SaveStateAsync();

        _logger.LogInformation("Time off approved for employee {EmployeeNumber} for tenant {TenantId}: {TimeOffId} by {ApprovedBy}", 
            _state.State.Employee.EmployeeNumber, GetTenantId(), request.TimeOffId, request.ApprovedBy);
    }

    public async Task RecordPerformanceReviewAsync(RecordPerformanceReviewRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.RecordPerformanceReview(
            request.ReviewDate,
            request.Rating,
            request.Notes);

        await SaveStateAsync();

        _logger.LogInformation("Performance review recorded for employee {EmployeeNumber} for tenant {TenantId}: {Rating} on {ReviewDate}", 
            _state.State.Employee.EmployeeNumber, GetTenantId(), request.Rating, request.ReviewDate);
    }

    public async Task PromoteAsync(PromoteRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        var newSalary = new Money(request.NewSalary, request.Currency);
        _state.State.Employee.Promote(request.NewPositionId, newSalary, request.EffectiveDate);

        await SaveStateAsync();

        _logger.LogInformation("Employee {EmployeeNumber} promoted to position {NewPositionId} for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, request.NewPositionId, GetTenantId());
    }

    public async Task TransferAsync(TransferRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.Transfer(
            request.NewDepartmentId,
            request.NewManagerId,
            request.EffectiveDate);

        await SaveStateAsync();

        _logger.LogInformation("Employee {EmployeeNumber} transferred to department {NewDepartmentId} for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, request.NewDepartmentId, GetTenantId());
    }

    public async Task TerminateAsync(TerminateRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.Terminate(request.TerminationDate, request.Reason);

        await SaveStateAsync();

        _logger.LogInformation("Employee {EmployeeNumber} terminated for tenant {TenantId}: {Reason}", 
            _state.State.Employee.EmployeeNumber, GetTenantId(), request.Reason);
    }

    public async Task SuspendAsync(SuspendRequest request)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.Suspend(request.SuspensionDate, request.Reason);

        await SaveStateAsync();

        _logger.LogInformation("Employee {EmployeeNumber} suspended for tenant {TenantId}: {Reason}", 
            _state.State.Employee.EmployeeNumber, GetTenantId(), request.Reason);
    }

    public async Task ReactivateAsync()
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        _state.State.Employee.Reactivate();

        await SaveStateAsync();

        _logger.LogInformation("Employee {EmployeeNumber} reactivated for tenant {TenantId}", 
            _state.State.Employee.EmployeeNumber, GetTenantId());
    }

    public Task<EmployeeStatistics> GetStatisticsAsync()
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        var employee = _state.State.Employee;
        var statistics = new EmployeeStatistics
        {
            EmployeeId = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = employee.FullName,
            Status = employee.Status,
            YearsOfService = employee.GetYearsOfService(),
            AvailableVacationDays = employee.GetAvailableVacationDays(),
            UsedVacationDaysThisYear = employee.GetUsedVacationDays(DateTime.Now.Year),
            BenefitCount = employee.Benefits.Count(b => b.IsActive),
            SkillCount = employee.Skills.Count,
            DocumentCount = employee.Documents.Count,
            TimeOffRequestsThisYear = employee.TimeOffRecords.Count(t => t.StartDate.Year == DateTime.Now.Year),
            PendingTimeOffRequests = employee.TimeOffRecords.Count(t => t.Status == TimeOffStatus.Pending),
            LastPerformanceReview = employee.LastPerformanceReview,
            LastPerformanceRating = employee.LastPerformanceRating,
            CurrentSalary = employee.BaseSalary,
            HourlyRate = employee.HourlyRate,
            PayrollFrequency = employee.PayrollFrequency,
            DepartmentId = employee.DepartmentId,
            PositionId = employee.PositionId,
            ManagerId = employee.ManagerId
        };

        return Task.FromResult(statistics);
    }

    public Task<List<EmployeeTimeOff>> GetTimeOffHistoryAsync()
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        var timeOffHistory = _state.State.Employee.TimeOffRecords.ToList();
        return Task.FromResult(timeOffHistory);
    }

    public Task<decimal> GetAvailableVacationDaysAsync()
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        var availableDays = _state.State.Employee.GetAvailableVacationDays();
        return Task.FromResult(availableDays);
    }

    public Task<decimal> GetUsedVacationDaysAsync(int year)
    {
        if (_state.State.Employee == null)
            throw new InvalidOperationException("Employee not found");

        var usedDays = _state.State.Employee.GetUsedVacationDays(year);
        return Task.FromResult(usedDays);
    }
}

[GenerateSerializer]
public class EmployeeState
{
    [Id(0)]
    public Employee? Employee { get; set; }
}

// Request DTOs
public record CreateEmployeeRequest(
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string Email,
    DateTime DateOfBirth,
    Gender Gender,
    DateTime HireDate,
    EmploymentType EmploymentType,
    decimal BaseSalary,
    string Currency,
    string? MiddleName = null,
    string? Phone = null,
    string? Mobile = null,
    MaritalStatus MaritalStatus = default,
    Guid? DepartmentId = null,
    Guid? PositionId = null,
    Guid? ManagerId = null,
    string? WorkLocation = null);

public record UpdatePersonalInfoRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    string Email,
    string? Phone,
    string? Mobile,
    MaritalStatus MaritalStatus);

public record UpdateIdentificationRequest(
    string? NationalId,
    string? TaxId,
    string? SocialSecurityNumber);

public record UpdateEmploymentRequest(
    Guid? DepartmentId,
    Guid? PositionId,
    Guid? ManagerId,
    string? WorkLocation,
    EmploymentType EmploymentType);

public record UpdateCompensationRequest(
    decimal BaseSalary,
    string Currency,
    decimal? HourlyRate,
    PayrollFrequency PayrollFrequency);

public record AddAddressRequest(
    string Type,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

public record AddEmergencyContactRequest(
    string Name,
    string Relationship,
    string Phone,
    string? Email = null);

public record EnrollBenefitRequest(
    Guid BenefitId,
    DateTime EnrollmentDate,
    decimal? EmployeeContribution = null);

public record UnenrollBenefitRequest(
    Guid BenefitId,
    DateTime UnenrollmentDate);

public record AddSkillRequest(
    string SkillName,
    SkillLevel Level,
    DateTime? CertificationDate = null,
    DateTime? ExpiryDate = null);

public record UpdateSkillLevelRequest(
    string SkillName,
    SkillLevel Level);

public record AddDocumentRequest(
    string DocumentType,
    string FileName,
    string FileUrl,
    DateTime ExpiryDate);

public record RequestTimeOffRequest(
    TimeOffType Type,
    DateTime StartDate,
    DateTime EndDate,
    decimal Days,
    string Reason);

public record ApproveTimeOffRequest(
    Guid TimeOffId,
    Guid ApprovedBy);

public record RecordPerformanceReviewRequest(
    DateTime ReviewDate,
    decimal Rating,
    string? Notes = null);

public record PromoteRequest(
    Guid NewPositionId,
    decimal NewSalary,
    string Currency,
    DateTime EffectiveDate);

public record TransferRequest(
    Guid NewDepartmentId,
    Guid? NewManagerId,
    DateTime EffectiveDate);

public record TerminateRequest(
    DateTime TerminationDate,
    string Reason);

public record SuspendRequest(
    DateTime SuspensionDate,
    string Reason);

public record EmployeeStatistics
{
    public Guid EmployeeId { get; init; }
    public string EmployeeNumber { get; init; }
    public string FullName { get; init; }
    public EmployeeStatus Status { get; init; }
    public int YearsOfService { get; init; }
    public decimal AvailableVacationDays { get; init; }
    public decimal UsedVacationDaysThisYear { get; init; }
    public int BenefitCount { get; init; }
    public int SkillCount { get; init; }
    public int DocumentCount { get; init; }
    public int TimeOffRequestsThisYear { get; init; }
    public int PendingTimeOffRequests { get; init; }
    public DateTime? LastPerformanceReview { get; init; }
    public decimal? LastPerformanceRating { get; init; }
    public Money CurrentSalary { get; init; }
    public Money? HourlyRate { get; init; }
    public PayrollFrequency PayrollFrequency { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? PositionId { get; init; }
    public Guid? ManagerId { get; init; }
}
using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.HumanResources.Domain.Entities;

public class Employee : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string EmployeeNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? MiddleName { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Mobile { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public MaritalStatus MaritalStatus { get; private set; }
    public string? NationalId { get; private set; }
    public string? TaxId { get; private set; }
    public string? SocialSecurityNumber { get; private set; }
    public DateTime HireDate { get; private set; }
    public DateTime? TerminationDate { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? PositionId { get; private set; }
    public Guid? ManagerId { get; private set; }
    public string? WorkLocation { get; private set; }
    public PayrollFrequency PayrollFrequency { get; private set; }
    public Money BaseSalary { get; private set; }
    public Money? HourlyRate { get; private set; }
    public bool IsActive { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public DateTime? LastPerformanceReview { get; private set; }
    public decimal? LastPerformanceRating { get; private set; }
    
    private readonly List<EmployeeAddress> _addresses = new();
    public IReadOnlyCollection<EmployeeAddress> Addresses => _addresses.AsReadOnly();
    
    private readonly List<EmployeeContact> _emergencyContacts = new();
    public IReadOnlyCollection<EmployeeContact> EmergencyContacts => _emergencyContacts.AsReadOnly();
    
    private readonly List<EmployeeBenefit> _benefits = new();
    public IReadOnlyCollection<EmployeeBenefit> Benefits => _benefits.AsReadOnly();
    
    private readonly List<EmployeeSkill> _skills = new();
    public IReadOnlyCollection<EmployeeSkill> Skills => _skills.AsReadOnly();
    
    private readonly List<EmployeeDocument> _documents = new();
    public IReadOnlyCollection<EmployeeDocument> Documents => _documents.AsReadOnly();
    
    private readonly List<EmployeeTimeOff> _timeOffRecords = new();
    public IReadOnlyCollection<EmployeeTimeOff> TimeOffRecords => _timeOffRecords.AsReadOnly();

    private Employee() { }

    public Employee(
        Guid tenantId,
        string employeeNumber,
        string firstName,
        string lastName,
        string email,
        DateTime dateOfBirth,
        Gender gender,
        DateTime hireDate,
        EmploymentType employmentType,
        Money baseSalary,
        string currency)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        EmployeeNumber = employeeNumber;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        HireDate = hireDate;
        EmploymentType = employmentType;
        BaseSalary = baseSalary;
        Status = EmployeeStatus.Active;
        MaritalStatus = MaritalStatus.Single;
        PayrollFrequency = PayrollFrequency.Monthly;
        IsActive = true;
        
        AddDomainEvent(new EmployeeHiredDomainEvent(Id, TenantId, EmployeeNumber, FullName, HireDate));
    }

    public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();

    public void UpdatePersonalInfo(
        string firstName,
        string lastName,
        string? middleName,
        string email,
        string? phone,
        string? mobile,
        MaritalStatus maritalStatus)
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
        Email = email;
        Phone = phone;
        Mobile = mobile;
        MaritalStatus = maritalStatus;
    }

    public void UpdateIdentification(string? nationalId, string? taxId, string? socialSecurityNumber)
    {
        NationalId = nationalId;
        TaxId = taxId;
        SocialSecurityNumber = socialSecurityNumber;
    }

    public void UpdateEmployment(
        Guid? departmentId,
        Guid? positionId,
        Guid? managerId,
        string? workLocation,
        EmploymentType employmentType)
    {
        DepartmentId = departmentId;
        PositionId = positionId;
        ManagerId = managerId;
        WorkLocation = workLocation;
        EmploymentType = employmentType;
    }

    public void UpdateCompensation(Money baseSalary, Money? hourlyRate, PayrollFrequency payrollFrequency)
    {
        BaseSalary = baseSalary;
        HourlyRate = hourlyRate;
        PayrollFrequency = payrollFrequency;
        
        AddDomainEvent(new EmployeeCompensationChangedDomainEvent(Id, TenantId, baseSalary, hourlyRate));
    }

    public void AddAddress(string type, string street, string city, string state, string postalCode, string country)
    {
        var address = new EmployeeAddress(Id, type, street, city, state, postalCode, country);
        _addresses.Add(address);
    }

    public void AddEmergencyContact(string name, string relationship, string phone, string? email = null)
    {
        var contact = new EmployeeContact(Id, name, relationship, phone, email);
        _emergencyContacts.Add(contact);
    }

    public void EnrollInBenefit(Guid benefitId, DateTime enrollmentDate, decimal? employeeContribution = null)
    {
        if (_benefits.Any(b => b.BenefitId == benefitId && b.IsActive))
            throw new BusinessRuleValidationException("Employee is already enrolled in this benefit");

        var benefit = new EmployeeBenefit(Id, benefitId, enrollmentDate, employeeContribution);
        _benefits.Add(benefit);
        
        AddDomainEvent(new EmployeeBenefitEnrollmentDomainEvent(Id, TenantId, benefitId, enrollmentDate));
    }

    public void UnenrollFromBenefit(Guid benefitId, DateTime unenrollmentDate)
    {
        var benefit = _benefits.FirstOrDefault(b => b.BenefitId == benefitId && b.IsActive);
        if (benefit != null)
        {
            benefit.Unenroll(unenrollmentDate);
            AddDomainEvent(new EmployeeBenefitUnenrollmentDomainEvent(Id, TenantId, benefitId, unenrollmentDate));
        }
    }

    public void AddSkill(string skillName, SkillLevel level, DateTime? certificationDate = null, DateTime? expiryDate = null)
    {
        var skill = new EmployeeSkill(Id, skillName, level, certificationDate, expiryDate);
        _skills.Add(skill);
    }

    public void UpdateSkillLevel(string skillName, SkillLevel level)
    {
        var skill = _skills.FirstOrDefault(s => s.SkillName == skillName);
        if (skill != null)
        {
            skill.UpdateLevel(level);
        }
    }

    public void AddDocument(string documentType, string fileName, string fileUrl, DateTime expiryDate)
    {
        var document = new EmployeeDocument(Id, documentType, fileName, fileUrl, expiryDate);
        _documents.Add(document);
    }

    public void RequestTimeOff(
        TimeOffType type,
        DateTime startDate,
        DateTime endDate,
        decimal days,
        string reason)
    {
        var timeOff = new EmployeeTimeOff(Id, type, startDate, endDate, days, reason);
        _timeOffRecords.Add(timeOff);
        
        AddDomainEvent(new EmployeeTimeOffRequestedDomainEvent(Id, TenantId, type, startDate, endDate, days));
    }

    public void ApproveTimeOff(Guid timeOffId, Guid approvedBy)
    {
        var timeOff = _timeOffRecords.FirstOrDefault(t => t.Id == timeOffId);
        if (timeOff != null)
        {
            timeOff.Approve(approvedBy);
            AddDomainEvent(new EmployeeTimeOffApprovedDomainEvent(Id, TenantId, timeOffId, approvedBy));
        }
    }

    public void RecordPerformanceReview(DateTime reviewDate, decimal rating, string? notes = null)
    {
        LastPerformanceReview = reviewDate;
        LastPerformanceRating = rating;
        
        AddDomainEvent(new EmployeePerformanceReviewDomainEvent(Id, TenantId, reviewDate, rating, notes));
    }

    public void Promote(Guid newPositionId, Money newSalary, DateTime effectiveDate)
    {
        var previousPositionId = PositionId;
        var previousSalary = BaseSalary;
        
        PositionId = newPositionId;
        BaseSalary = newSalary;
        
        AddDomainEvent(new EmployeePromotedDomainEvent(
            Id, TenantId, previousPositionId, newPositionId, previousSalary, newSalary, effectiveDate));
    }

    public void Transfer(Guid newDepartmentId, Guid? newManagerId, DateTime effectiveDate)
    {
        var previousDepartmentId = DepartmentId;
        var previousManagerId = ManagerId;
        
        DepartmentId = newDepartmentId;
        ManagerId = newManagerId;
        
        AddDomainEvent(new EmployeeTransferredDomainEvent(
            Id, TenantId, previousDepartmentId, newDepartmentId, previousManagerId, newManagerId, effectiveDate));
    }

    public void Terminate(DateTime terminationDate, string reason)
    {
        TerminationDate = terminationDate;
        Status = EmployeeStatus.Terminated;
        IsActive = false;
        
        AddDomainEvent(new EmployeeTerminatedDomainEvent(Id, TenantId, terminationDate, reason));
    }

    public void Suspend(DateTime suspensionDate, string reason)
    {
        Status = EmployeeStatus.Suspended;
        AddDomainEvent(new EmployeeSuspendedDomainEvent(Id, TenantId, suspensionDate, reason));
    }

    public void Reactivate()
    {
        Status = EmployeeStatus.Active;
        IsActive = true;
        AddDomainEvent(new EmployeeReactivatedDomainEvent(Id, TenantId));
    }

    public int GetYearsOfService()
    {
        var endDate = TerminationDate ?? DateTime.UtcNow;
        return (int)((endDate - HireDate).TotalDays / 365.25);
    }

    public decimal GetAvailableVacationDays()
    {
        var yearsOfService = GetYearsOfService();
        var baseVacationDays = 15; // Base vacation days
        var additionalDays = Math.Min(yearsOfService, 10) * 1; // 1 additional day per year, max 10
        return baseVacationDays + additionalDays;
    }

    public decimal GetUsedVacationDays(int year)
    {
        return _timeOffRecords
            .Where(t => t.Type == TimeOffType.Vacation && 
                       t.Status == TimeOffStatus.Approved && 
                       t.StartDate.Year == year)
            .Sum(t => t.Days);
    }
}

// Supporting entities
public class EmployeeAddress : Entity<Guid>
{
    public Guid EmployeeId { get; private set; }
    public string Type { get; private set; }
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }
    public bool IsDefault { get; private set; }

    private EmployeeAddress() { }

    public EmployeeAddress(Guid employeeId, string type, string street, string city, string state, string postalCode, string country)
    {
        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        Type = type;
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }
}

public class EmployeeContact : Entity<Guid>
{
    public Guid EmployeeId { get; private set; }
    public string Name { get; private set; }
    public string Relationship { get; private set; }
    public string Phone { get; private set; }
    public string? Email { get; private set; }

    private EmployeeContact() { }

    public EmployeeContact(Guid employeeId, string name, string relationship, string phone, string? email)
    {
        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        Name = name;
        Relationship = relationship;
        Phone = phone;
        Email = email;
    }
}

public class EmployeeBenefit : Entity<Guid>
{
    public Guid EmployeeId { get; private set; }
    public Guid BenefitId { get; private set; }
    public DateTime EnrollmentDate { get; private set; }
    public DateTime? UnenrollmentDate { get; private set; }
    public decimal? EmployeeContribution { get; private set; }
    public bool IsActive { get; private set; }

    private EmployeeBenefit() { }

    public EmployeeBenefit(Guid employeeId, Guid benefitId, DateTime enrollmentDate, decimal? employeeContribution)
    {
        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        BenefitId = benefitId;
        EnrollmentDate = enrollmentDate;
        EmployeeContribution = employeeContribution;
        IsActive = true;
    }

    public void Unenroll(DateTime unenrollmentDate)
    {
        UnenrollmentDate = unenrollmentDate;
        IsActive = false;
    }
}

public class EmployeeSkill : Entity<Guid>
{
    public Guid EmployeeId { get; private set; }
    public string SkillName { get; private set; }
    public SkillLevel Level { get; private set; }
    public DateTime? CertificationDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    private EmployeeSkill() { }

    public EmployeeSkill(Guid employeeId, string skillName, SkillLevel level, DateTime? certificationDate, DateTime? expiryDate)
    {
        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        SkillName = skillName;
        Level = level;
        CertificationDate = certificationDate;
        ExpiryDate = expiryDate;
    }

    public void UpdateLevel(SkillLevel level)
    {
        Level = level;
    }
}

public class EmployeeDocument : Entity<Guid>
{
    public Guid EmployeeId { get; private set; }
    public string DocumentType { get; private set; }
    public string FileName { get; private set; }
    public string FileUrl { get; private set; }
    public DateTime UploadDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    private EmployeeDocument() { }

    public EmployeeDocument(Guid employeeId, string documentType, string fileName, string fileUrl, DateTime? expiryDate)
    {
        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        DocumentType = documentType;
        FileName = fileName;
        FileUrl = fileUrl;
        UploadDate = DateTime.UtcNow;
        ExpiryDate = expiryDate;
    }
}

public class EmployeeTimeOff : Entity<Guid>
{
    public Guid EmployeeId { get; private set; }
    public TimeOffType Type { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal Days { get; private set; }
    public string Reason { get; private set; }
    public TimeOffStatus Status { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime RequestedAt { get; private set; }

    private EmployeeTimeOff() { }

    public EmployeeTimeOff(Guid employeeId, TimeOffType type, DateTime startDate, DateTime endDate, decimal days, string reason)
    {
        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Days = days;
        Reason = reason;
        Status = TimeOffStatus.Pending;
        RequestedAt = DateTime.UtcNow;
    }

    public void Approve(Guid approvedBy)
    {
        Status = TimeOffStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = TimeOffStatus.Rejected;
    }
}

// Enums
public class Gender : SmartEnum<Gender>
{
    public static readonly Gender Male = new(1, nameof(Male));
    public static readonly Gender Female = new(2, nameof(Female));
    public static readonly Gender Other = new(3, nameof(Other));

    private Gender(int value, string name) : base(name, value) { }
}

public class MaritalStatus : SmartEnum<MaritalStatus>
{
    public static readonly MaritalStatus Single = new(1, nameof(Single));
    public static readonly MaritalStatus Married = new(2, nameof(Married));
    public static readonly MaritalStatus Divorced = new(3, nameof(Divorced));
    public static readonly MaritalStatus Widowed = new(4, nameof(Widowed));

    private MaritalStatus(int value, string name) : base(name, value) { }
}

public class EmployeeStatus : SmartEnum<EmployeeStatus>
{
    public static readonly EmployeeStatus Active = new(1, nameof(Active));
    public static readonly EmployeeStatus Inactive = new(2, nameof(Inactive));
    public static readonly EmployeeStatus Suspended = new(3, nameof(Suspended));
    public static readonly EmployeeStatus Terminated = new(4, nameof(Terminated));

    private EmployeeStatus(int value, string name) : base(name, value) { }
}

public class EmploymentType : SmartEnum<EmploymentType>
{
    public static readonly EmploymentType FullTime = new(1, nameof(FullTime));
    public static readonly EmploymentType PartTime = new(2, nameof(PartTime));
    public static readonly EmploymentType Contract = new(3, nameof(Contract));
    public static readonly EmploymentType Temporary = new(4, nameof(Temporary));
    public static readonly EmploymentType Intern = new(5, nameof(Intern));

    private EmploymentType(int value, string name) : base(name, value) { }
}

public class PayrollFrequency : SmartEnum<PayrollFrequency>
{
    public static readonly PayrollFrequency Weekly = new(1, nameof(Weekly));
    public static readonly PayrollFrequency BiWeekly = new(2, nameof(BiWeekly));
    public static readonly PayrollFrequency Monthly = new(3, nameof(Monthly));
    public static readonly PayrollFrequency Quarterly = new(4, nameof(Quarterly));

    private PayrollFrequency(int value, string name) : base(name, value) { }
}

public class SkillLevel : SmartEnum<SkillLevel>
{
    public static readonly SkillLevel Beginner = new(1, nameof(Beginner));
    public static readonly SkillLevel Intermediate = new(2, nameof(Intermediate));
    public static readonly SkillLevel Advanced = new(3, nameof(Advanced));
    public static readonly SkillLevel Expert = new(4, nameof(Expert));

    private SkillLevel(int value, string name) : base(name, value) { }
}

public class TimeOffType : SmartEnum<TimeOffType>
{
    public static readonly TimeOffType Vacation = new(1, nameof(Vacation));
    public static readonly TimeOffType Sick = new(2, nameof(Sick));
    public static readonly TimeOffType Personal = new(3, nameof(Personal));
    public static readonly TimeOffType Maternity = new(4, nameof(Maternity));
    public static readonly TimeOffType Paternity = new(5, nameof(Paternity));
    public static readonly TimeOffType Bereavement = new(6, nameof(Bereavement));

    private TimeOffType(int value, string name) : base(name, value) { }
}

public class TimeOffStatus : SmartEnum<TimeOffStatus>
{
    public static readonly TimeOffStatus Pending = new(1, nameof(Pending));
    public static readonly TimeOffStatus Approved = new(2, nameof(Approved));
    public static readonly TimeOffStatus Rejected = new(3, nameof(Rejected));
    public static readonly TimeOffStatus Cancelled = new(4, nameof(Cancelled));

    private TimeOffStatus(int value, string name) : base(name, value) { }
}

// Domain Events
public record EmployeeHiredDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    string EmployeeNumber,
    string FullName,
    DateTime HireDate) : INotification;

public record EmployeeTerminatedDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    DateTime TerminationDate,
    string Reason) : INotification;

public record EmployeeCompensationChangedDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    Money NewBaseSalary,
    Money? NewHourlyRate) : INotification;

public record EmployeeBenefitEnrollmentDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    Guid BenefitId,
    DateTime EnrollmentDate) : INotification;

public record EmployeeBenefitUnenrollmentDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    Guid BenefitId,
    DateTime UnenrollmentDate) : INotification;

public record EmployeeTimeOffRequestedDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    TimeOffType Type,
    DateTime StartDate,
    DateTime EndDate,
    decimal Days) : INotification;

public record EmployeeTimeOffApprovedDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    Guid TimeOffId,
    Guid ApprovedBy) : INotification;

public record EmployeePerformanceReviewDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    DateTime ReviewDate,
    decimal Rating,
    string? Notes) : INotification;

public record EmployeePromotedDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    Guid? PreviousPositionId,
    Guid NewPositionId,
    Money PreviousSalary,
    Money NewSalary,
    DateTime EffectiveDate) : INotification;

public record EmployeeTransferredDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    Guid? PreviousDepartmentId,
    Guid NewDepartmentId,
    Guid? PreviousManagerId,
    Guid? NewManagerId,
    DateTime EffectiveDate) : INotification;

public record EmployeeSuspendedDomainEvent(
    Guid EmployeeId,
    Guid TenantId,
    DateTime SuspensionDate,
    string Reason) : INotification;

public record EmployeeReactivatedDomainEvent(
    Guid EmployeeId,
    Guid TenantId) : INotification;
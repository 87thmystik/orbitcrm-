using OrbitCRM.Domain.Enums;

namespace OrbitCRM.Application.Leads.DTOs;

public class LeadDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? Source { get; set; }
    public LeadStatus Status { get; set; }
    public Priority Priority { get; set; }
    public decimal EstimatedValue { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateLeadDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? Source { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public decimal EstimatedValue { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLeadDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? Source { get; set; }
    public LeadStatus Status { get; set; }
    public Priority Priority { get; set; }
    public decimal EstimatedValue { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
}
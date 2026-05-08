namespace FSBS.Infrastructure.Persistence.Repositories.Interfaces;

/// <summary>Lightweight projection of an organisation's ID and display name.</summary>
public record OrganisationSummary(Guid Id, string Name);

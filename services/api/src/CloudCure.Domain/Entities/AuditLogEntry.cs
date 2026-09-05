using CloudCure.Domain.Enums;

namespace CloudCure.Domain.Entities;

/// <summary>
/// One row per change to an audited entity, written in the same transaction as the change
/// itself. The old app had no audit trail at all despite handling PHI.
/// </summary>
public class AuditLogEntry
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? ActorPersonId { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public AuditAction Action { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? CorrelationId { get; set; }
}

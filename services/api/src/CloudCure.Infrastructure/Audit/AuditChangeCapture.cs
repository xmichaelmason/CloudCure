using System.Text.Json;
using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CloudCure.Infrastructure.Audit;

/// <summary>
/// Captures before/after snapshots of audited entities around a save. Split into two phases
/// because a newly-inserted row's generated primary key isn't known until after the database
/// round-trip completes — see <see cref="CloudCureDbContextAuditExtensions"/> for how the two
/// phases are combined into a single transaction.
/// </summary>
public static class AuditChangeCapture
{
    private static readonly HashSet<Type> AuditedTypes =
    [
        typeof(Person),
        typeof(Patient),
        typeof(StaffMember),
        typeof(PersonRole),
        typeof(Encounter),
        typeof(Diagnosis),
        typeof(Vitals),
        typeof(Assessment),
        typeof(Screening),
        typeof(PatientAllergy),
        typeof(PatientCondition),
        typeof(PatientMedication),
        typeof(PatientSurgery),
    ];

    public static List<PendingAudit> CaptureBeforeSave(ChangeTracker changeTracker)
    {
        var pending = new List<PendingAudit>();

        foreach (var entry in changeTracker.Entries())
        {
            if (!AuditedTypes.Contains(entry.Entity.GetType()) || entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Insert,
                EntityState.Modified => AuditAction.Update,
                EntityState.Deleted => AuditAction.Delete,
                _ => throw new InvalidOperationException($"Unexpected entity state {entry.State} for audited entity."),
            };

            string? beforeJson = action == AuditAction.Insert ? null : SerializeValues(entry, useOriginalValues: true);
            string? afterJson = action == AuditAction.Delete ? null : SerializeValues(entry, useOriginalValues: false);

            pending.Add(new PendingAudit(entry, entry.Entity.GetType().Name, action, beforeJson, afterJson));
        }

        return pending;
    }

    public static IReadOnlyList<AuditLogEntry> BuildAuditEntries(
        IReadOnlyList<PendingAudit> pending, ICurrentRequestContext requestContext, DateTimeOffset occurredAt)
    {
        return pending
            .Select(p => new AuditLogEntry
            {
                OccurredAt = occurredAt,
                ActorPersonId = requestContext.ActorPersonId,
                EntityType = p.EntityType,
                EntityId = FormatEntityId(p.Entry),
                Action = p.Action,
                BeforeJson = p.BeforeJson,
                AfterJson = p.AfterJson,
                CorrelationId = requestContext.CorrelationId,
            })
            .ToList();
    }

    private static string FormatEntityId(EntityEntry entry)
    {
        var keyValues = entry.Metadata.FindPrimaryKey()!.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "null");

        return string.Join(":", keyValues);
    }

    private static string SerializeValues(EntityEntry entry, bool useOriginalValues)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            values[property.Metadata.Name] = useOriginalValues ? property.OriginalValue : property.CurrentValue;
        }

        return JsonSerializer.Serialize(values);
    }
}

public sealed record PendingAudit(EntityEntry Entry, string EntityType, AuditAction Action, string? BeforeJson, string? AfterJson);

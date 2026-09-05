using CloudCure.Domain.Entities;
using CloudCure.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;

namespace CloudCure.Infrastructure.Data;

public class CloudCureDbContext(DbContextOptions<CloudCureDbContext> options, ICurrentRequestContext currentRequestContext)
    : DbContext(options)
{
    public DbSet<Person> People => Set<Person>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<AuthIdentity> AuthIdentities => Set<AuthIdentity>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<PersonRole> PersonRoles => Set<PersonRole>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();

    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Vitals> Vitals => Set<Vitals>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<BodyRegion> BodyRegions => Set<BodyRegion>();
    public DbSet<AssessmentPainPoint> AssessmentPainPoints => Set<AssessmentPainPoint>();

    public DbSet<ScreeningTemplate> ScreeningTemplates => Set<ScreeningTemplate>();
    public DbSet<ScreeningQuestion> ScreeningQuestions => Set<ScreeningQuestion>();
    public DbSet<Screening> Screenings => Set<Screening>();
    public DbSet<ScreeningAnswer> ScreeningAnswers => Set<ScreeningAnswer>();

    public DbSet<AllergyReference> AllergyReferences => Set<AllergyReference>();
    public DbSet<ConditionReference> ConditionReferences => Set<ConditionReference>();
    public DbSet<MedicationReference> MedicationReferences => Set<MedicationReference>();
    public DbSet<SurgeryReference> SurgeryReferences => Set<SurgeryReference>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<PatientCondition> PatientConditions => Set<PatientCondition>();
    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();
    public DbSet<PatientSurgery> PatientSurgeries => Set<PatientSurgery>();

    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CloudCureDbContext).Assembly);
    }

    /// <summary>
    /// Wraps every save in a transaction that also inserts the audit rows describing it, so an
    /// audit row can never exist without the change it describes, or vice versa. Two internal
    /// save round-trips are needed because a newly-inserted row's generated id isn't known
    /// until after the first one completes.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var pending = AuditChangeCapture.CaptureBeforeSave(ChangeTracker);
        if (pending.Count == 0)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);

            var affected = await base.SaveChangesAsync(cancellationToken);

            var auditEntries = AuditChangeCapture.BuildAuditEntries(pending, currentRequestContext, DateTimeOffset.UtcNow);
            AuditLog.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return affected;
        });
    }

    public override int SaveChanges() => SaveChangesAsync().GetAwaiter().GetResult();
}

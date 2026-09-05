namespace CloudCure.Infrastructure.Audit;

/// <summary>
/// Who is making the current request, for attribution on audit log rows. Implemented against
/// the internal JWT's subject claim once auth is wired up (phase 3); until then
/// <see cref="NullCurrentRequestContext"/> covers design-time tooling and tests that construct
/// the DbContext directly.
/// </summary>
public interface ICurrentRequestContext
{
    Guid? ActorPersonId { get; }
    string? CorrelationId { get; }
}

public sealed class NullCurrentRequestContext : ICurrentRequestContext
{
    public static readonly NullCurrentRequestContext Instance = new();

    public Guid? ActorPersonId => null;
    public string? CorrelationId => null;
}

using CloudCure.Domain.Enums;

namespace CloudCure.Domain.Workflow;

/// <summary>
/// The one legal path an encounter's stage may advance through. No skipping ahead,
/// no going backward — this is what replaces the old app's implicit, nested
/// template-conditional workflow logic with a single testable source of truth.
/// </summary>
public static class EncounterWorkflow
{
    private static readonly IReadOnlyDictionary<EncounterStage, EncounterStage> NextStageByCurrent =
        new Dictionary<EncounterStage, EncounterStage>
        {
            [EncounterStage.Registered] = EncounterStage.VitalsPending,
            [EncounterStage.VitalsPending] = EncounterStage.AssessmentPending,
            [EncounterStage.AssessmentPending] = EncounterStage.AwaitingDoctor,
            [EncounterStage.AwaitingDoctor] = EncounterStage.Finalized,
        };

    public static EncounterStage? NextStage(EncounterStage from) =>
        NextStageByCurrent.TryGetValue(from, out var next) ? next : null;

    public static bool CanTransition(EncounterStage from, EncounterStage to) =>
        NextStage(from) == to;

    public static void EnsureCanTransition(EncounterStage from, EncounterStage to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidEncounterTransitionException(from, to);
        }
    }
}

public sealed class InvalidEncounterTransitionException(EncounterStage from, EncounterStage to)
    : InvalidOperationException($"Cannot transition an encounter from {from} to {to}.")
{
    public EncounterStage From { get; } = from;
    public EncounterStage To { get; } = to;
}

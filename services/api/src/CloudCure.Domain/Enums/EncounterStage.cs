namespace CloudCure.Domain.Enums;

/// <summary>
/// Explicit clinical-encounter workflow state machine — replaces the old app's implicit
/// "compute next step from nested template conditionals on data shape" logic.
/// </summary>
public enum EncounterStage
{
    Registered = 1,
    VitalsPending = 2,
    AssessmentPending = 3,
    AwaitingDoctor = 4,
    Finalized = 5,
}

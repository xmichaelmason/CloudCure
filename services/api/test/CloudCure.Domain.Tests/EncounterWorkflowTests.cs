using CloudCure.Domain.Enums;
using CloudCure.Domain.Workflow;

namespace CloudCure.Domain.Tests;

public class EncounterWorkflowTests
{
    [Theory]
    [InlineData(EncounterStage.Registered, EncounterStage.VitalsPending)]
    [InlineData(EncounterStage.VitalsPending, EncounterStage.AssessmentPending)]
    [InlineData(EncounterStage.AssessmentPending, EncounterStage.AwaitingDoctor)]
    [InlineData(EncounterStage.AwaitingDoctor, EncounterStage.Finalized)]
    public void CanTransition_allows_the_single_legal_next_step(EncounterStage from, EncounterStage to)
    {
        Assert.True(EncounterWorkflow.CanTransition(from, to));
    }

    [Theory]
    [InlineData(EncounterStage.Registered, EncounterStage.AssessmentPending)]
    [InlineData(EncounterStage.Registered, EncounterStage.AwaitingDoctor)]
    [InlineData(EncounterStage.Registered, EncounterStage.Finalized)]
    [InlineData(EncounterStage.VitalsPending, EncounterStage.AwaitingDoctor)]
    [InlineData(EncounterStage.VitalsPending, EncounterStage.Finalized)]
    [InlineData(EncounterStage.VitalsPending, EncounterStage.Registered)]
    [InlineData(EncounterStage.AssessmentPending, EncounterStage.Finalized)]
    [InlineData(EncounterStage.AwaitingDoctor, EncounterStage.VitalsPending)]
    [InlineData(EncounterStage.Finalized, EncounterStage.Registered)]
    public void CanTransition_rejects_skipped_or_backward_steps(EncounterStage from, EncounterStage to)
    {
        Assert.False(EncounterWorkflow.CanTransition(from, to));
    }

    [Fact]
    public void CanTransition_rejects_transitioning_to_the_same_stage()
    {
        Assert.False(EncounterWorkflow.CanTransition(EncounterStage.VitalsPending, EncounterStage.VitalsPending));
    }

    [Fact]
    public void Finalized_has_no_legal_next_stage()
    {
        Assert.False(EncounterWorkflow.CanTransition(EncounterStage.Finalized, EncounterStage.Registered));
        Assert.Null(EncounterWorkflow.NextStage(EncounterStage.Finalized));
    }

    [Fact]
    public void EnsureCanTransition_throws_with_a_message_naming_both_stages_on_an_illegal_transition()
    {
        var ex = Assert.Throws<InvalidEncounterTransitionException>(
            () => EncounterWorkflow.EnsureCanTransition(EncounterStage.Registered, EncounterStage.Finalized));

        Assert.Contains("Registered", ex.Message);
        Assert.Contains("Finalized", ex.Message);
    }

    [Fact]
    public void EnsureCanTransition_does_not_throw_on_a_legal_transition()
    {
        EncounterWorkflow.EnsureCanTransition(EncounterStage.VitalsPending, EncounterStage.AssessmentPending);
    }
}

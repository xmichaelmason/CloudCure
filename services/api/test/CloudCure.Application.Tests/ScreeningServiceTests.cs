using CloudCure.Application.Screenings;
using CloudCure.Domain.Entities;
using CloudCure.Infrastructure.Audit;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CloudCure.Application.Tests;

/// <summary>
/// Verifies the screening questionnaire is genuinely data-driven — the old app hardcoded
/// `question1..question5` as fixed columns, so adding or reordering a question required a
/// schema migration. Here it must not.
/// </summary>
public class ScreeningServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("cloudcure_v2_test")
        .WithUsername("cloudcure")
        .WithPassword("cloudcure")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private CloudCureDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CloudCureDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CloudCureDbContext(options, NullCurrentRequestContext.Instance);
    }

    private async Task<Guid> CreatePersonAsync(CloudCureDbContext context, string firstName = "Test", string lastName = "Nurse")
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        context.People.Add(person);
        await context.SaveChangesAsync();
        return person.Id;
    }

    private async Task<int> CreatePatientAsync(CloudCureDbContext context)
    {
        var personId = await CreatePersonAsync(context, "Test", "Patient");
        var patient = new Patient { PersonId = personId };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return patient.Id;
    }

    [Fact]
    public async Task GetTemplateAsync_returns_the_seeded_covid_template_with_questions_in_display_order()
    {
        await using var context = CreateContext();
        var service = new ScreeningService(context);

        var template = await service.GetTemplateAsync("covid19_v1");

        Assert.NotNull(template);
        Assert.Equal("COVID-19 Screening", template!.Title);
        Assert.Equal(5, template.Questions.Count);
        for (var i = 0; i < template.Questions.Count; i++)
        {
            Assert.Equal(i + 1, template.Questions[i].DisplayOrder);
        }
    }

    [Fact]
    public async Task GetTemplateAsync_returns_null_for_an_unknown_template_code()
    {
        await using var context = CreateContext();
        var service = new ScreeningService(context);

        Assert.Null(await service.GetTemplateAsync("does-not-exist"));
    }

    [Fact]
    public async Task GetTemplateAsync_reflects_a_question_added_purely_as_data_with_no_code_change()
    {
        await using var context = CreateContext();
        var template = await context.ScreeningTemplates.SingleAsync(t => t.Code == "covid19_v1");
        context.ScreeningQuestions.Add(new ScreeningQuestion
        {
            ScreeningTemplateId = template.Id,
            DisplayOrder = 6,
            QuestionText = "Have you received a COVID-19 vaccine booster in the last 6 months?",
            AnswerType = Domain.Enums.ScreeningAnswerType.YesNo,
        });
        await context.SaveChangesAsync();

        var service = new ScreeningService(context);
        var result = await service.GetTemplateAsync("covid19_v1");

        Assert.Equal(6, result!.Questions.Count);
        Assert.Contains(result.Questions, q => q.QuestionText.Contains("booster"));
        Assert.Equal(6, result.Questions[5].DisplayOrder);
    }

    [Fact]
    public async Task SubmitAsync_persists_the_screening_and_all_answers_atomically()
    {
        await using var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var nurseId = await CreatePersonAsync(context, "Florence", "Nightingale");
        var template = await context.ScreeningTemplates.Include(t => t.Questions).SingleAsync(t => t.Code == "covid19_v1");
        var service = new ScreeningService(context);

        var answers = template.Questions
            .Select(q => new ScreeningAnswerInput(q.Id, AnswerBool: false, AnswerText: null, AnswerNumber: null))
            .ToList();

        var screeningId = await service.SubmitAsync(patientId, "covid19_v1", nurseId, null, answers);

        var savedAnswers = await context.ScreeningAnswers.Where(a => a.ScreeningId == screeningId).ToListAsync();
        Assert.Equal(template.Questions.Count, savedAnswers.Count);
        Assert.All(savedAnswers, a => Assert.Equal(false, a.AnswerBool));
    }

    [Fact]
    public async Task SubmitAsync_throws_for_an_unknown_template_code_and_persists_nothing()
    {
        await using var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var service = new ScreeningService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitAsync(patientId, "not-a-template", Guid.NewGuid(), null, []));

        Assert.Equal(0, await context.Screenings.CountAsync());
    }
}

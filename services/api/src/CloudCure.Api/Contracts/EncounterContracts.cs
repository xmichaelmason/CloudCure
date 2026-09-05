namespace CloudCure.Api.Contracts;

public record CreateEncounterRequest(int PatientId);

public record EncounterCreatedResponse(int EncounterId);

public record VitalsApiInput(
    int SystolicMmHg,
    int DiastolicMmHg,
    decimal OxygenSaturationPct,
    int HeartRateBpm,
    int RespiratoryRateBpm,
    decimal TemperatureValue,
    string TemperatureUnit,
    decimal HeightValue,
    string HeightUnit,
    decimal WeightValue,
    string WeightUnit);

public record AssessmentApiInput(string ChiefComplaint, string HistoryOfPresentIllness, int PainScale, IReadOnlyList<int> PainBodyRegionIds);

public record FinalizeDiagnosisRequest(string DoctorDiagnosis, string RecommendedTreatment);

public record AssignDoctorRequest(int StaffMemberId);

public record VitalsResponse(
    int SystolicMmHg, int DiastolicMmHg, decimal OxygenSaturationPct, int HeartRateBpm, int RespiratoryRateBpm,
    decimal TemperatureValue, string TemperatureUnit, decimal HeightValue, string HeightUnit, decimal WeightValue, string WeightUnit);

public record AssessmentResponse(string ChiefComplaint, string HistoryOfPresentIllness, int PainScale, IReadOnlyList<string> PainBodyRegions);

public record DiagnosisResponse(string? DoctorDiagnosis, string? RecommendedTreatment, string? FinalizedByName, DateTimeOffset? FinalizedAt);

public record EncounterDetailResponse(
    int EncounterId,
    int PatientId,
    string PatientName,
    string Stage,
    string? AttendingDoctorName,
    VitalsResponse? Vitals,
    AssessmentResponse? Assessment,
    DiagnosisResponse? Diagnosis);

public record BodyRegionResponse(int Id, string Code, string DisplayName, string RegionGroup);

public record StaffSearchResult(int StaffMemberId, string FirstName, string LastName, string Specialization);

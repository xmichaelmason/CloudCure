using CloudCure.Domain.Enums;

namespace CloudCure.Domain.Entities;

/// <summary>1:1 with an encounter. Explicit unit columns fix the old app's unit-less raw doubles.</summary>
public class Vitals
{
    public int Id { get; set; }
    public int EncounterId { get; set; }
    public int SystolicMmHg { get; set; }
    public int DiastolicMmHg { get; set; }
    public decimal OxygenSaturationPct { get; set; }
    public int HeartRateBpm { get; set; }
    public int RespiratoryRateBpm { get; set; }
    public decimal TemperatureValue { get; set; }
    public TemperatureUnit TemperatureUnit { get; set; }
    public decimal HeightValue { get; set; }
    public HeightUnit HeightUnit { get; set; }
    public decimal WeightValue { get; set; }
    public WeightUnit WeightUnit { get; set; }

    public Encounter Encounter { get; set; } = null!;
}

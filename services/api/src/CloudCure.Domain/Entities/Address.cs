namespace CloudCure.Domain.Entities;

/// <summary>Decomposed mailing address — replaces the old app's single free-text `Address` string.</summary>
public class Address
{
    public int Id { get; set; }
    public Guid PersonId { get; set; }
    public required string Line1 { get; set; }
    public string? Line2 { get; set; }
    public required string City { get; set; }
    public required string StateProvince { get; set; }
    public required string PostalCode { get; set; }
    public required string CountryCode { get; set; }

    public Person Person { get; set; } = null!;
}

namespace CloudCure.Domain.Entities;

/// <summary>Links an Auth0 subject to an internal Person, so Auth0 identity changes never cascade through the schema.</summary>
public class AuthIdentity
{
    public int Id { get; set; }
    public Guid PersonId { get; set; }
    public required string Auth0Subject { get; set; }

    public Person Person { get; set; } = null!;
}

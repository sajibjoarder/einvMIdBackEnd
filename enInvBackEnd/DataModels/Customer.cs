using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class Customer
{
    public Guid Id { get; set; }

    public string? Tin { get; set; }

    public string? Brn { get; set; }

    public string? Sst { get; set; }

    public string? Ttx { get; set; }

    public string? CityName { get; set; }

    public string? PostalZone { get; set; }

    public string? CountrySubentityCodeStateCode { get; set; }

    public string? Address { get; set; }

    public string? CountryCode { get; set; }

    public string? CompanyName { get; set; }

    public string? CompanyId { get; set; }

    public string? Telephone { get; set; }

    public string? Email { get; set; }

    public Guid? UserId { get; set; }
}

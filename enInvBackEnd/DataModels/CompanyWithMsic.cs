using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class CompanyWithMsic
{
    public Guid? CompanyId { get; set; }

    public string? CompanyName { get; set; }

    public string? AddressL1 { get; set; }

    public string? AddressL2 { get; set; }

    public string? ContectNumber { get; set; }

    public string? Email { get; set; }

    public string? IdType { get; set; }

    public string? SstNumber { get; set; }

    public string? IdNumber { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Country { get; set; }

    public string? TourismTaxNo { get; set; }

    public string? Taxid { get; set; }

    public string? StateCode { get; set; }

    public int? PostCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? RegistratedCompanyId { get; set; }

    public Guid? MsicCodeId { get; set; }

    public int? MsicCode { get; set; }

    public string? MsicName { get; set; }
}

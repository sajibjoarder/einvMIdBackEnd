using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class Invoice
{
    public Guid Id { get; set; }

    public DateTime? TimeSummitted { get; set; }

    public string? Type { get; set; }

    public string? Ststus { get; set; }

    public string? RespososeDetails { get; set; }

    public int? ResposeCode { get; set; }

    public string? Path { get; set; }

    public Guid? CompanyId { get; set; }

    public string? InvoiceId { get; set; }

    public string? SubmissionId { get; set; }

    public string? DocId { get; set; }
}

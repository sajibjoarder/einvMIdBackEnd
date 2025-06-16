using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class MsicCode
{
    public Guid Id { get; set; }

    public int? Code { get; set; }

    public string? Name { get; set; }

    public Guid? CompanyId { get; set; }
}

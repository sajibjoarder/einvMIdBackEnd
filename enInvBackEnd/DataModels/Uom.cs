using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class Uom
{
    public Guid Uomif { get; set; }

    public string? UomName { get; set; }

    public string? UomType { get; set; }

    public Guid? CompanyId { get; set; }
}

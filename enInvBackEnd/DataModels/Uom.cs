using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class Uom
{
    public string? UomName { get; set; }

    public string? UomValue { get; set; }

    public Guid UomId { get; set; }
}

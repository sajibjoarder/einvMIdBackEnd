using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class LhdnToken
{
    public Guid TokenId { get; set; }

    public Guid? CompanyId { get; set; }

    public DateTime? ExpieryDateTime { get; set; }

    public DateTime? IssueddateTime { get; set; }

    public string? Token { get; set; }
}

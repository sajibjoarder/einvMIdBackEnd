using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class CompanyUser
{
    public Guid Id { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? UserId { get; set; }
}

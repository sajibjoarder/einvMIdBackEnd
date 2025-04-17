using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class UserType
{
    public Guid Id { get; set; }

    public string? Type { get; set; }
}

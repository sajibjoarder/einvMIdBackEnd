using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class LhdnProfile
{
    public Guid Id { get; set; }

    public Guid? CompanyId { get; set; }

    public string? ClientIdLhdn { get; set; }

    public string? ClientSecretLhdn { get; set; }

    public string GrantTypeLhdn { get; set; } = null!;

    public string ScopeLhdn { get; set; } = null!;

    public string IntrigrationType { get; set; } = null!;
}

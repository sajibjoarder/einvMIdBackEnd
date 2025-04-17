using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string? PhoneNo { get; set; }

    public string Roll { get; set; } = null!;

    public string Nname { get; set; } = null!;
}

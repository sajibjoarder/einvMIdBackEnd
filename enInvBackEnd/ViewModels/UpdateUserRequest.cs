
public class UpdateUserRequest
{
    public string CurrentPassword { get; set; } = null!;   // REQUIRED – for verification

    // Optional fields you may change
    public string? Email { get; set; }
    public string? PhoneNo { get; set; }
    public string? Roll { get; set; }
    public string? NewPassword { get; set; }                // Optional, sets a new password
}


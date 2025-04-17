using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;

namespace enInvBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PasswordRecoveryController : ControllerBase
    {
        private readonly string _secretKey = "YourSuperSecretKey123!"; // Store securely, e.g., in environment variables

        [HttpPost("request-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] ResetRequestModel model)
        {
            using (var context = new EninvContext())
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                // Generate a tamper-proof reset token
                string token = GenerateResetToken(user.Email);

                // Send email with the reset link
                string resetUrl = $"https://yourfrontend.com/reset-password?token={token}";
                await SendPasswordResetEmail(user.Email, user.Nname, resetUrl);

                return Ok(new { message = "Password reset email sent." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ValidateResetToken(model.Token, out string email))
            {
                return BadRequest(new { message = "Invalid or expired token." });
            }

            using (var context = new EninvContext())
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                // Hash the new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

                context.Users.Update(user);
                await context.SaveChangesAsync();

                return Ok(new { message = "Password has been reset successfully." });
            }
        }

        private string GenerateResetToken(string email)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
            {
                string expiryTime = DateTime.UtcNow.AddHours(1).Ticks.ToString();
                string dataToSign = $"{email}|{expiryTime}";
                string hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign)));

                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{dataToSign}|{hash}"));
                return token;
            }
        }

        private bool ValidateResetToken(string token, out string email)
        {
            email = string.Empty;

            try
            {
                string decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                string[] parts = decodedToken.Split('|');

                if (parts.Length != 3)
                {
                    return false;
                }

                email = parts[0];
                long expiryTicks = long.Parse(parts[1]);
                string providedHash = parts[2];

                if (DateTime.UtcNow.Ticks > expiryTicks)
                {
                    return false; // Token has expired
                }

                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
                {
                    string dataToSign = $"{email}|{expiryTicks}";
                    string computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign)));

                    return providedHash == computedHash; // Verify integrity
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task SendPasswordResetEmail(string toEmail, string userName, string resetUrl)
        {
            try
            {
                // Email sender details (same as referenced page)
                string senderEmail = "sajibjoarder@gmail.com";
                string senderPassword = "cxts vuqm zrho blut"; // Google App Password

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Math Society", senderEmail));
                message.To.Add(new MailboxAddress(userName, toEmail));
                message.Subject = "Password Reset Request";

                // Construct email body
                string emailBody = string.Format(@"
                <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                            .header {{ background-color: #007bff; color: white; padding: 10px; text-align: center; }}
                            .content {{ padding: 20px; }}
                            .footer {{ font-size: 12px; color: gray; text-align: center; margin-top: 20px; }}
                            .reset-btn {{ display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>Password Reset Request</h2>
                            </div>
                            <div class='content'>
                                <p>Hello {0},</p>
                                <p>We received a request to reset your password. Click the button below to reset your password:</p>
                                <p style='text-align:center;'>
                                    <a class='reset-btn' href='{1}'>Reset Password</a>
                                </p>
                                <p>This link is valid for 1 hour. If you did not request a password reset, please ignore this email.</p>
                            </div>
                            <div class='footer'>
                                <p>&copy; {2} Math Society. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                </html>", userName, resetUrl, DateTime.UtcNow.Year);

                var bodyBuilder = new BodyBuilder { HtmlBody = emailBody };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(senderEmail, senderPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send email: {ex.Message}");
            }
        }
    }

    public class ResetRequestModel
    {
        public string Email { get; set; } = null!;
    }

    public class ResetPasswordModel
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}

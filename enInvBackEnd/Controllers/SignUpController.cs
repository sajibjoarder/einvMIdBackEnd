using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using enInvBackEnd.DataModels;
using enInvBackEnd.DataContext;
using enInvBackEnd.ViewModel;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace enInvBackEnd.Controllers
{
    [Route("api/signup")]
    [ApiController]
    public class SignUpController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest signUpRequest)
        {
            if (signUpRequest == null)
            {
                return BadRequest(new { message = "Request is null." });
            }

            if (string.IsNullOrEmpty(signUpRequest.Email) ||
                string.IsNullOrEmpty(signUpRequest.Password) ||
                string.IsNullOrEmpty(signUpRequest.Name))
            {
                return BadRequest(new { message = "Name, Email, and Password are required." });
            }

            using (var context = new EninvContext()) // Creating DbContext instance inside the method
            {
                // Check if email is already registered
                var existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Email == signUpRequest.Email);

                if (existingUser != null)
                {
                    return BadRequest(new { message = "Email already used." });
                }

                // Hash password using BCrypt
                string hashedPassword = HashPassword(signUpRequest.Password);

                var user = new User
                {
                    Email = signUpRequest.Email,
                    PasswordHash = hashedPassword,
                    Roll = "User",
                    Nname = signUpRequest.Name
                };

                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            return Ok(new { message = "User registered successfully." });
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}

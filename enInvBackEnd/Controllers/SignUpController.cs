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
using Microsoft.AspNetCore.Authorization;

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



        [Route("api/users")]
        [ApiController]
        [Authorize(Roles = "User")] // Only Admin and User roles can access this controller
        public class UsersController : ControllerBase
        {
            // PUT: api/users/{id}
            [HttpPut("{id}")]
            public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest req)
            {
                if (req == null || string.IsNullOrWhiteSpace(req.CurrentPassword))
                    return BadRequest(new { message = "CurrentPassword is required." });

                using (var db = new EninvContext())
                {
                    var user = await db.Users.FindAsync(id);
                    if (user == null)
                        return NotFound(new { message = "User not found." });

                    // 1. Verify current password
                    if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
                        return Unauthorized(new { message = "Current password is incorrect." });

                    // 2. Email update (remains unique)
                    if (!string.IsNullOrEmpty(req.Email) && req.Email != user.Email)
                    {
                        bool emailUsed = await db.Users.AnyAsync(u => u.Email == req.Email && u.Id != id);
                        if (emailUsed)
                            return BadRequest(new { message = "Email already in use." });

                        user.Email = req.Email;
                    }

                    // 3. Phone
                    if (!string.IsNullOrEmpty(req.PhoneNo))
                        user.PhoneNo = req.PhoneNo;

                    // 4. Roll
               

                    // 5. New password (hash before save)
                    if (!string.IsNullOrWhiteSpace(req.NewPassword))
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

                    await db.SaveChangesAsync();
                }

                return Ok(new { message = "User updated successfully." });
            }
        }


        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }


        public class UpdateUserRequest
        {
            public string CurrentPassword { get; set; } = null!;   // REQUIRED – for verification

            // Optional fields you may change
            public string? Email { get; set; }
            public string? PhoneNo { get; set; }
            public string? NewPassword { get; set; }                // Optional, sets a new password
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using enInvBackEnd.DataContext;
using enInvBackEnd.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

using static enInvBackEnd.Controllers.SignUpController;
using Microsoft.AspNetCore.Authorization;

namespace enInvBackEnd.Controllers
{
    [Route("api/users")]
    [ApiController]
   // [Authorize(Roles = "User")] // Only User roles can access this controller
    public class UsersController : ControllerBase
    {


        // GET: api/users/{id}
        [HttpGet("{uid}")]
        public async Task<IActionResult> GetUser(Guid uid)
        {
            using (var db = new EninvContext())
            {
                var user = await db.Users.FirstOrDefaultAsync(x=>x.Id==uid);

                if (user == null)
                    return NotFound(new { message = "User not found." });

                return Ok(user);
            }
        }




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
                if (!string.IsNullOrEmpty(req.Roll))
                    user.Roll = req.Roll;

                // 5. New password (hash before save)
                if (!string.IsNullOrWhiteSpace(req.NewPassword))
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

                await db.SaveChangesAsync();
            }

            return Ok(new { message = "User updated successfully." });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using enInvBackEnd.DataModels;
using System;
using System.Linq;
using enInvBackEnd.DataContext;

namespace enInvBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LhdnProfileController : ControllerBase
    {
        // CREATE
        [HttpPost("create")]
        public IActionResult CreateLhdnProfile([FromBody] LhdnProfile profile)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    profile.Id = Guid.NewGuid();
                    db.LhdnProfiles.Add(profile);
                    db.SaveChanges();

                    return Ok(new { message = "LHDN Profile created successfully." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // READ (Get by ID)
        [HttpGet("get/{id}")]
        public IActionResult GetLhdnProfile(Guid id)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var profile = db.LhdnProfiles.FirstOrDefault(p => p.Id == id);
                    if (profile == null)
                        return NotFound("LHDN Profile not found.");

                    return Ok(profile);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // READ (Get all profiles for a company)
        [HttpGet("get-by-company/{companyId}")]
        public IActionResult GetProfilesByCompany(Guid companyId)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var profiles = db.LhdnProfiles.Where(p => p.CompanyId == companyId).ToList();
                    return Ok(profiles);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // UPDATE
        [HttpPut("update/{id}")]
        public IActionResult UpdateLhdnProfile(Guid id, [FromBody] LhdnProfile updatedProfile)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var profile = db.LhdnProfiles.FirstOrDefault(p => p.Id == id);
                    if (profile == null)
                        return NotFound("LHDN Profile not found.");

                    // Update fields
                    profile.CompanyId = updatedProfile.CompanyId;
                    profile.ClientIdLhdn = updatedProfile.ClientIdLhdn;
                    profile.ClientSecretLhdn = updatedProfile.ClientSecretLhdn;
                    profile.GrantTypeLhdn = updatedProfile.GrantTypeLhdn;
                    profile.ScopeLhdn = updatedProfile.ScopeLhdn;
                    profile.IntrigrationType = updatedProfile.IntrigrationType;

                    db.SaveChanges();

                    return Ok(new { message = "LHDN Profile updated successfully." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE
        [HttpDelete("delete/{id}")]
        public IActionResult DeleteLhdnProfile(Guid id)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var profile = db.LhdnProfiles.FirstOrDefault(p => p.Id == id);
                    if (profile == null)
                        return NotFound("LHDN Profile not found.");

                    db.LhdnProfiles.Remove(profile);
                    db.SaveChanges();

                    return Ok(new { message = "LHDN Profile deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

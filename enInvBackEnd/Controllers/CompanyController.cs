using Microsoft.AspNetCore.Mvc;
using enInvBackEnd.DataModels;
using System;
using System.Linq;
using enInvBackEnd.DataContext;

namespace enInvBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        // CREATE
        [HttpPost("create-company-for-user")]
        public IActionResult CreateCompanyForUser(Guid userId, [FromBody] CompanyDetail companyDetail)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var user = db.Users.Find(userId);
                    if (user == null)
                        return NotFound("User not found.");

                    companyDetail.CompanyId = Guid.NewGuid();
                    companyDetail.CreatedAt = DateTime.UtcNow;
                    db.CompanyDetails.Add(companyDetail);
                    db.SaveChanges();

                    var companyUser = new CompanyUser
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        CompanyId = companyDetail.CompanyId
                    };
                    db.CompanyUsers.Add(companyUser);
                    db.SaveChanges();

                    return Ok(new { message = "Company created and linked to user successfully." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // READ (Get single company by ID)
        [HttpGet("get-company/{companyId}")]
        public IActionResult GetCompany(Guid companyId)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var company = db.CompanyDetails.FirstOrDefault(c => c.CompanyId == companyId);
                    if (company == null)
                        return NotFound("Company not found.");

                    return Ok(company);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // READ (Get all companies linked to a user)
        [HttpGet("get-companies-for-user/{userId}")]
        public IActionResult GetCompaniesForUser(Guid userId)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var companyIds = db.CompanyUsers
                        .Where(cu => cu.UserId == userId)
                        .Select(cu => cu.CompanyId)
                        .ToList();

                    var companies = db.CompanyDetails
                        .Where(c => companyIds.Contains(c.CompanyId))
                        .ToList();

                    return Ok(companies);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // UPDATE
        [HttpPut("update-company/{companyId}")]
        public IActionResult UpdateCompany(Guid companyId, [FromBody] CompanyDetail updatedCompany)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var company = db.CompanyDetails.FirstOrDefault(c => c.CompanyId == companyId);
                    if (company == null)
                        return NotFound("Company not found.");

                    // Update fields
                    company.CompanyName = updatedCompany.CompanyName;
                    company.AddressL1 = updatedCompany.AddressL1;
                    company.AddressL2 = updatedCompany.AddressL2;
                    company.ContectNumber = updatedCompany.ContectNumber;
                    company.Email = updatedCompany.Email;
                    company.IdType = updatedCompany.IdType;
                    company.SstNumber = updatedCompany.SstNumber;
                    company.IdNumber = updatedCompany.IdNumber;
                    company.City = updatedCompany.City;
                    company.State = updatedCompany.State;
                    company.Country = updatedCompany.Country;
                    company.TourismTaxNo = updatedCompany.TourismTaxNo;
                    company.Taxid = updatedCompany.Taxid;
                    company.StateCode = updatedCompany.StateCode;
                    company.PostCode = updatedCompany.PostCode;
                    company.MsicCode = updatedCompany.MsicCode;
                    // Do not update CreatedAt

                    db.SaveChanges();

                    return Ok(new { message = "Company updated successfully." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE
        [HttpDelete("delete-company/{companyId}")]
        public IActionResult DeleteCompany(Guid companyId)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var company = db.CompanyDetails.FirstOrDefault(c => c.CompanyId == companyId);
                    if (company == null)
                        return NotFound("Company not found.");

                    // Remove linked CompanyUser(s)
                    var companyUsers = db.CompanyUsers.Where(cu => cu.CompanyId == companyId).ToList();
                    db.CompanyUsers.RemoveRange(companyUsers);

                    db.CompanyDetails.Remove(company);
                    db.SaveChanges();

                    return Ok(new { message = "Company and its links deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

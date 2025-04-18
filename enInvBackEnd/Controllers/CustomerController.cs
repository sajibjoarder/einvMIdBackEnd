using Microsoft.AspNetCore.Mvc;
using enInvBackEnd.DataModels;
using System;
using System.Linq;
using enInvBackEnd.DataContext;

namespace enInvBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        // CREATE Customer for a User
        [HttpPost("create-customer-for-user")]
        public IActionResult CreateCustomerForUser(Guid userId, [FromBody] Customer customer)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var user = db.Users.Find(userId);
                    if (user == null)
                        return NotFound("User not found.");

                    customer.Id = Guid.NewGuid();
                    customer.UserId = userId;

                    db.Customers.Add(customer);
                    db.SaveChanges();

                    return Ok(new { message = "Customer created successfully linked to user." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // READ Single Customer by ID
        [HttpGet("get-customer/{customerId}")]
        public IActionResult GetCustomer(Guid customerId)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var customer = db.Customers.FirstOrDefault(c => c.Id == customerId);
                    if (customer == null)
                        return NotFound("Customer not found.");

                    return Ok(customer);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // READ All Customers for a User
        [HttpGet("get-customers-for-user/{userId}")]
        public IActionResult GetCustomersForUser(Guid userId)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var customers = db.Customers.Where(c => c.UserId == userId).ToList();
                    return Ok(customers);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // UPDATE Customer
        [HttpPut("update-customer/{customerId}")]
        public IActionResult UpdateCustomer(Guid customerId, [FromBody] Customer updatedCustomer)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var customer = db.Customers.FirstOrDefault(c => c.Id == customerId);
                    if (customer == null)
                        return NotFound("Customer not found.");

                    customer.Tin = updatedCustomer.Tin;
                    customer.Brn = updatedCustomer.Brn;
                    customer.Sst = updatedCustomer.Sst;
                    customer.Ttx = updatedCustomer.Ttx;
                    customer.CityName = updatedCustomer.CityName;
                    customer.PostalZone = updatedCustomer.PostalZone;
                    customer.CountrySubentityCodeStateCode = updatedCustomer.CountrySubentityCodeStateCode;
                    customer.Address = updatedCustomer.Address;
                    customer.CountryCode = updatedCustomer.CountryCode;
                    customer.CompanyName = updatedCustomer.CompanyName;
                    customer.CompanyId = updatedCustomer.CompanyId;
                    customer.Telephone = updatedCustomer.Telephone;
                    customer.Email = updatedCustomer.Email;
                    // Note: Normally UserId should not be updated once set

                    db.SaveChanges();

                    return Ok(new { message = "Customer updated successfully." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE Customer
        [HttpDelete("delete-customer/{customerId}")]
        public IActionResult DeleteCustomer(Guid customerId)
        {
            try
            {
                using (var db = new EninvContext())
                {
                    var customer = db.Customers.FirstOrDefault(c => c.Id == customerId);
                    if (customer == null)
                        return NotFound("Customer not found.");

                    db.Customers.Remove(customer);
                    db.SaveChanges();

                    return Ok(new { message = "Customer deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

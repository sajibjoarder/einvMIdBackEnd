using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;

namespace enInvBackEnd.Api.Controllers
{
    /// <summary>
    /// Exposes MSIC codes (Malaysia Standard Industrial Classification) to callers.
    /// </summary>
    [Route("api/[controller]")]     // → GET /api/msiccodes
    [ApiController]
    public class MsicCodesController : ControllerBase
    {
        /// <summary>
        /// Returns every MSIC code in the table.
        /// </summary>
        [HttpGet]                   // GET /api/msiccodes
        public async Task<IActionResult> GetAll()
        {
            try
            {
             
                using (var db = new EninvContext())   // replace with your own DbContext name if different
                {
                    // AsNoTracking() is faster for read-only queries.
                    List<MsicCode> data = await db.MsicCodes
                                                  .AsNoTracking()
                                                  .ToListAsync();

                    return Ok(data);                 // 200 + JSON array
                }
            }
            catch (Exception ex)
            {
                // TODO: log the exception (Serilog, ILogger, etc.)
                return StatusCode(500, new { message = "Failed to retrieve MSIC codes.", detail = ex.Message });
            }
        }

        
    }
}


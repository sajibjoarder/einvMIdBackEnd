using Microsoft.AspNetCore.Mvc;
using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;
using CsvHelper;
using System.Globalization;

namespace enInvBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MsicCodeController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll([FromQuery] Guid? companyId = null)
        {
            using var db = new EninvContext();
            var q = db.MsicCodes.AsQueryable();
            if (companyId != null) q = q.Where(m => m.CompanyId == companyId);
            return Ok(q.ToList());
        }

        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            using var db = new EninvContext();
            var m = db.MsicCodes.Find(id);
            return m is null ? NotFound() : Ok(m);
        }

        [HttpPost]
        public IActionResult Create([FromBody] MsicCode code)
        {
            if (code == null) return BadRequest();
            code.Id = Guid.NewGuid();

            using var db = new EninvContext();
            db.MsicCodes.Add(code);
            db.SaveChanges();
            return CreatedAtAction(nameof(Get), new { id = code.Id }, code);
        }

        [HttpPut("{id}")]
        public IActionResult Update(Guid id, [FromBody] MsicCode updated)
        {
            if (updated == null || id != updated.Id) return BadRequest();

            using var db = new EninvContext();
            var m = db.MsicCodes.Find(id);
            if (m == null) return NotFound();

            m.Code = updated.Code;
            m.Name = updated.Name;
            m.CompanyId = updated.CompanyId;
            db.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            using var db = new EninvContext();
            var m = db.MsicCodes.Find(id);
            if (m == null) return NotFound();
            db.MsicCodes.Remove(m);
            db.SaveChanges();
            return NoContent();
        }

        [HttpPost("bulk-upload")]
        [Consumes("multipart/form-data")]
        public IActionResult BulkUpload(IFormFile file, [FromQuery] Guid companyId)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            List<MsicCode> list;
            using (var rdr = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(rdr, CultureInfo.InvariantCulture))
            {
                list = csv.GetRecords<MsicCode>().ToList();
            }

            foreach (var m in list)
            {
                m.Id = Guid.NewGuid();
                m.CompanyId = companyId;
            }

            using var db = new EninvContext();
            db.MsicCodes.AddRange(list);
            db.SaveChanges();
            return Ok(new { count = list.Count });
        }
    }
}

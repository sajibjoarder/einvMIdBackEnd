using Microsoft.AspNetCore.Mvc;
using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;
using CsvHelper;
using System.Globalization;

namespace enInvBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UomController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            using var db = new EninvContext();
            return Ok(db.Uoms.ToList());
        }

        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            using var db = new EninvContext();
            var u = db.Uoms.Find(id);
            return u is null ? NotFound() : Ok(u);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Uom uom)
        {
            if (uom == null) return BadRequest();
            uom.UomId = Guid.NewGuid();

            using var db = new EninvContext();
            db.Uoms.Add(uom);
            db.SaveChanges();
            return CreatedAtAction(nameof(Get), new { id = uom.UomId }, uom);
        }

        [HttpPut("{id}")]
        public IActionResult Update(Guid id, [FromBody] Uom updated)
        {
            if (updated == null || id != updated.UomId) return BadRequest();

            using var db = new EninvContext();
            var u = db.Uoms.Find(id);
            if (u == null) return NotFound();

            u.UomName = updated.UomName;
            u.UomValue = updated.UomValue;
            db.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            using var db = new EninvContext();
            var u = db.Uoms.Find(id);
            if (u == null) return NotFound();
            db.Uoms.Remove(u);
            db.SaveChanges();
            return NoContent();
        }

        [HttpPost("bulk-upload")]
        [Consumes("multipart/form-data")]
        public IActionResult BulkUpload(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            List<Uom> list;
            using (var rdr = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(rdr, CultureInfo.InvariantCulture))
            {
                list = csv.GetRecords<Uom>().ToList();
            }

            foreach (var u in list)
            {
                u.UomId = Guid.NewGuid();
            }

            using var db = new EninvContext();
            db.Uoms.AddRange(list);
            db.SaveChanges();
            return Ok(new { count = list.Count });
        }
    }
}

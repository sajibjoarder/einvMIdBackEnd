using Microsoft.AspNetCore.Mvc;
using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;
using CsvHelper;
using System.Globalization;

namespace enInvBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        // GET: api/Product?companyId=...
        [HttpGet]
        public IActionResult GetAll([FromQuery] Guid? companyId = null)
        {
            using var db = new EninvContext();
            var q = db.Products.AsQueryable();
            if (companyId != null) q = q.Where(p => p.CompanyId == companyId);
            return Ok(q.ToList());
        }

        // GET: api/Product/{id}
        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            using var db = new EninvContext();
            var p = db.Products.Find(id);
            return p is null ? NotFound() : Ok(p);
        }

        // POST: api/Product
        [HttpPost]
        public IActionResult Create([FromBody] Product product)
        {
            if (product == null) return BadRequest();
            product.ProductId = Guid.NewGuid();

            using var db = new EninvContext();
            db.Products.Add(product);
            db.SaveChanges();
            return CreatedAtAction(nameof(Get), new { id = product.ProductId }, product);
        }

        // PUT: api/Product/{id}
        [HttpPut("{id}")]
        public IActionResult Update(Guid id, [FromBody] Product updated)
        {
            if (updated == null || id != updated.ProductId) return BadRequest();

            using var db = new EninvContext();
            var p = db.Products.Find(id);
            if (p == null) return NotFound();

            p.ProductName = updated.ProductName;
            p.ItemClassificationCode = updated.ItemClassificationCode;
            p.Uom = updated.Uom;
            p.Price = updated.Price;
            p.Quantity = updated.Quantity;
            p.CompanyId = updated.CompanyId;
            db.SaveChanges();
            return NoContent();
        }

        // DELETE: api/Product/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            using var db = new EninvContext();
            var p = db.Products.Find(id);
            if (p == null) return NotFound();
            db.Products.Remove(p);
            db.SaveChanges();
            return NoContent();
        }

        // POST: api/Product/bulk-upload?companyId=...
        [HttpPost("bulk-upload")]
        [Consumes("multipart/form-data")]
        public IActionResult BulkUpload(IFormFile file, [FromQuery] Guid companyId)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            List<Product> list;
            using (var rdr = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(rdr, CultureInfo.InvariantCulture))
            {
                list = csv.GetRecords<Product>().ToList();
            }

            foreach (var p in list)
            {
                p.ProductId = Guid.NewGuid();
                p.CompanyId = companyId;
            }

            using var db = new EninvContext();
            db.Products.AddRange(list);
            db.SaveChanges();
            return Ok(new { count = list.Count });
        }
    }
}

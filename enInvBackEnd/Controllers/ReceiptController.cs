using Microsoft.AspNetCore.Mvc;
using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;

namespace enInvBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReceiptController : ControllerBase
    {
        // GET: api/Receipt
        //[HttpGet]
        //public IActionResult GetAll()
        //{
        //    using var db = new EninvContext();
        //    var receipts = db.Receipts.ToList();
        //    return Ok(receipts);
        //}

        // GET: api/Receipt/{id}
        [HttpGet("{id}")]
        public IActionResult GetById(Guid id)
        {
            using var db = new EninvContext();
            var receipt = db.Receipts
                            .Where(r => r.ReceiptId == id)
                            .Select(r => new
                            {
                                r.ReceiptId,
                                r.ReceiptNumber,
                                r.DateOfIssue,
                                r.SellerName,
                                r.SellerLogoUrl,
                                r.SellerAddress,
                                r.SellerContact,
                                r.BuyerName,
                                r.BuyerAddress,
                                r.Subtotal,
                                r.Discount,
                                r.Tax,
                                r.TotalAmount,
                                r.PaymentMethod,
                                r.PaymentReferenceId,
                                r.CreatedAt,
                                r.CompanyId,
                                Items = r.ReceiptItems.Select(i => new
                                {
                                    i.ItemId,
                                    i.ItemDescription,
                                    i.Quantity,
                                    i.UnitPrice
                                })
                            })
                            .FirstOrDefault();

            return receipt is null ? NotFound() : Ok(receipt);
        }

        // GET: api/Receipt/range?startDate=yyyy-mm-dd&endDate=yyyy-mm-dd
        [HttpGet("range")]
        public IActionResult GetByDateRange([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate)
        {
            using var db = new EninvContext();
            var receipts = db.Receipts
                             .Where(r => r.DateOfIssue >= startDate && r.DateOfIssue <= endDate)
                             .ToList();
            return Ok(receipts);
        }

        // GET: api/Receipt/date?date=yyyy-mm-dd
        [HttpGet("date")]
        public IActionResult GetBySingleDate([FromQuery] DateOnly date)
        {
            using var db = new EninvContext();
            var receipts = db.Receipts
                             .Where(r => r.DateOfIssue == date)
                             .ToList();
            return Ok(receipts);
        }

        // POST: api/Receipt
        [HttpPost]
        public IActionResult Create([FromBody] Receipt receipt)
        {
            if (receipt == null || receipt.ReceiptItems == null) return BadRequest();

            receipt.ReceiptId = Guid.NewGuid();
            receipt.CreatedAt = DateTime.Now;

            foreach (var item in receipt.ReceiptItems)
            {
                item.ItemId = Guid.NewGuid();
                item.ReceiptId = receipt.ReceiptId;
            }

            using var db = new EninvContext();
            db.Receipts.Add(receipt);
            db.SaveChanges();
            return CreatedAtAction(nameof(GetById), new { id = receipt.ReceiptId }, receipt);
        }

        // PUT: api/Receipt/{id}
        [HttpPut("{id}")]
        public IActionResult Update(Guid id, [FromBody] Receipt updated)
        {
            if (updated == null || id != updated.ReceiptId) return BadRequest();

            using var db = new EninvContext();
            var existing = db.Receipts.Find(id);
            if (existing == null) return NotFound();

            existing.ReceiptNumber = updated.ReceiptNumber;
            existing.DateOfIssue = updated.DateOfIssue;
            existing.SellerName = updated.SellerName;
            existing.SellerLogoUrl = updated.SellerLogoUrl;
            existing.SellerAddress = updated.SellerAddress;
            existing.SellerContact = updated.SellerContact;
            existing.BuyerName = updated.BuyerName;
            existing.BuyerAddress = updated.BuyerAddress;
            existing.Subtotal = updated.Subtotal;
            existing.Discount = updated.Discount;
            existing.Tax = updated.Tax;
            existing.TotalAmount = updated.TotalAmount;
            existing.PaymentMethod = updated.PaymentMethod;
            existing.PaymentReferenceId = updated.PaymentReferenceId;
            existing.CompanyId = updated.CompanyId;

            db.SaveChanges();
            return NoContent();
        }

        // DELETE: api/Receipt/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            using var db = new EninvContext();
            var receipt = db.Receipts.Find(id);
            if (receipt == null) return NotFound();

            db.Receipts.Remove(receipt);
            db.SaveChanges();
            return NoContent();
        }
    }
}

using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;
using enInvBackEnd.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace enInvBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReceiptController : ControllerBase
    {
        // GET: api/Receipt
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var db = new EninvContext();
            var receipts = await db.Receipts.ToListAsync();
            return Ok(receipts);
        }

        // GET: api/Receipt/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            using var db = new EninvContext();
            var receipt = await db.Receipts
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
                                      r.Submitted,
                                      r.Docid,
                                      r.DocType,
                                      Items = r.ReceiptItems.Select(i => new
                                      {
                                          i.ItemId,
                                          i.ItemDescription,
                                          i.Quantity,
                                          i.UnitPrice
                                      })
                                  })
                                  .FirstOrDefaultAsync();

            return receipt is null ? NotFound() : Ok(receipt);
        }

        // GET: api/Receipt/range?startDate=yyyy-mm-dd&endDate=yyyy-mm-dd&companyId=guid
        [HttpGet("range")]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, [FromQuery] Guid? companyId)
        {
            using var db = new EninvContext();
            var query = db.Receipts
                          .Where(r => r.DateOfIssue >= startDate && r.DateOfIssue <= endDate);

            if (companyId.HasValue)
                query = query.Where(r => r.CompanyId == companyId);

            var receipts = await query.ToListAsync();
            return Ok(receipts);
        }

        // GET: api/Receipt/date?date=yyyy-mm-dd&companyId=guid
        [HttpGet("date")]
        public async Task<IActionResult> GetBySingleDate([FromQuery] DateOnly date, [FromQuery] Guid? companyId)
        {
            using var db = new EninvContext();
            var query = db.Receipts.Where(r => r.DateOfIssue == date);

            if (companyId.HasValue)
                query = query.Where(r => r.CompanyId == companyId);

            var receipts = await query.ToListAsync();
            return Ok(receipts);
        }


        // POST: api/Receipt
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Receipt receipt)
        {
            if (receipt == null || receipt.ReceiptItems == null)
                return BadRequest();

            using var db = new EninvContext();

            // Check for duplicate ReceiptNumber
            bool exists = await db.Receipts.AnyAsync(r => r.ReceiptNumber == receipt.ReceiptNumber);
            if (exists)
                return Conflict(new { message = "A receipt with this ReceiptNumber already exists." });

            receipt.ReceiptId = Guid.NewGuid();
            receipt.CreatedAt = DateTime.Now;

            foreach (var item in receipt.ReceiptItems)
            {
                item.ItemId = Guid.NewGuid();
                item.ReceiptId = receipt.ReceiptId;
            }

            db.Receipts.Add(receipt);
            await db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = receipt.ReceiptId }, receipt);
        }

        // PUT: api/Receipt/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Receipt updated)
        {
            if (updated == null || id != updated.ReceiptId)
                return BadRequest();

            using var db = new EninvContext();
            var existing = await db.Receipts.FindAsync(id);
            if (existing == null)
                return NotFound();

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
            existing.Submitted = updated.Submitted;
            existing.Docid = updated.Docid;
            existing.DocType = updated.DocType;

            await db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Receipt/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            using var db = new EninvContext();
            var receipt = await db.Receipts.FindAsync(id);
            if (receipt == null)
                return NotFound();

            db.Receipts.Remove(receipt);
            await db.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/Receipt/update-status
        [HttpPatch("update-status")]
        public async Task<IActionResult> BulkUpdateStatus([FromBody] List<ReceiptUpdateModel> updates)
        {
            if (updates == null || updates.Count == 0)
                return BadRequest("No updates provided.");

            using var db = new EninvContext();
            var receiptIds = updates.Select(u => u.ReceiptId).ToList();
            var receipts = await db.Receipts
                                   .Where(r => receiptIds.Contains(r.ReceiptId))
                                   .ToListAsync();

            foreach (var receipt in receipts)
            {
                var update = updates.First(u => u.ReceiptId == receipt.ReceiptId);
                receipt.Submitted = update.Submitted;
                receipt.Docid = update.Docid;
                receipt.DocType = update.DocType;
            }

            await db.SaveChangesAsync();
            return Ok(new { updated = receipts.Count });
        }

    }
}

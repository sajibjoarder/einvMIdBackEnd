// Controllers/CreditNotesController.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using enInvBackEnd.CreditNotes;          // <-- DTOs & builder live here
using enInvBackEnd.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace enInvBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreditNotesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly DocumentSubmissionService _svc;

        public CreditNotesController(IWebHostEnvironment env,
                                     DocumentSubmissionService svc)
        {
            _env = env;
            _svc = svc;
        }

        // POST api/creditnotes/submit/{company_id}
        [HttpPost("submit/{company_id}")]
        public async Task<IActionResult> Submit(Guid company_id,
            [FromBody] CreditNoteModel dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var xml = new UblCreditNoteBuilder().Build(dto);

            var dir = Path.Combine(_env.ContentRootPath, "credit-notes");
            Directory.CreateDirectory(dir);

            var safeId = string.Concat((dto.Id ?? "CN")
                           .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
            var file = $"{safeId}_{Guid.NewGuid():N}.xml";
            var path = Path.Combine(dir, file);

            await using (var fs = System.IO.File.Create(path))
                xml.Save(fs);

            HttpResponseMessage resp =
                await _svc.SubmitXmlAsync(path, dto.SupplierTaxId, company_id);
            var respBody = await resp.Content.ReadAsStringAsync();

            return Created(string.Empty, new { file, path, respBody });
        }
    }
}

// ──────────────────────────────────────────────────────────────
//  Everything below is in a DIFFERENT namespace, so it cannot
//  clash with the earlier Invoice / Consolidation types.
// ──────────────────────────────────────────────────────────────
namespace enInvBackEnd.CreditNotes
{
    #region DTOs
    public sealed class CreditNoteModel
    {
        [Required] public string Id { get; set; } = "";
        [Required] public DateTime IssueDate { get; set; }
        public TimeSpan IssueTime { get; set; } = TimeSpan.Zero;
        public string InvoiceTypeCode { get; set; } = "02";   // credit note
        public string TypeCodeVer { get; set; } = "1.0";
        [Required] public string CurrencyCode { get; set; } = "MYR";
        [Required] public string TaxCurrencyCode { get; set; } = "MYR";

        public List<BillingReference> BillingRefs { get; set; } = new();
        public List<AdditionalDoc> AdditionalDocs { get; set; } = new();

        [Required] public Party Supplier { get; set; } = new();
        [Required] public Party Customer { get; set; } = new();

        [Required] public TaxTotal TaxTotal { get; set; } = new();
        [Required] public MonetaryTotal MonetaryTotal { get; set; } = new();

        [Required] public List<CreditLine> Lines { get; set; } = new();

        [Required] public string SupplierTaxId { get; set; } = "";
    }

    public sealed class BillingReference { public string Id { get; set; } = ""; }

    public sealed class AdditionalDoc
    {
        [Required] public string Id { get; set; } = "";
        public string? DocumentType { get; set; }
        public string? Description { get; set; }
    }

    public sealed class Party
    {
        public List<PartyId> Identifications { get; set; } = new();
        public Address? Address { get; set; }
        public LegalEntity Legal { get; set; } = new();
        public Contact? Contact { get; set; }
    }

    public sealed class PartyId { public string Scheme = ""; public string Value = ""; }
    public sealed class Address { public string City = ""; public string Postal = ""; public string State = ""; public string Line = ""; public string Country = ""; }
    public sealed class LegalEntity { public string RegistrationName = ""; public string? CompanyId; }
    public sealed class Contact { public string Telephone = ""; public string Email = ""; }

    public sealed class TaxTotal
    {
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string TaxCategoryId { get; set; } = "01";
        public string TaxSchemeId { get; set; } = "OTH";
    }
    public sealed class MonetaryTotal
    {
        public decimal LineExtensionAmount { get; set; }
        public decimal TaxExclusiveAmount { get; set; }
        public decimal TaxInclusiveAmount { get; set; }
        public decimal AllowanceTotalAmount { get; set; }
        public decimal ChargeTotalAmount { get; set; }
        public decimal PayableAmount { get; set; }
    }

    public sealed class CreditLine
    {
        [Required] public string Id { get; set; } = "";
        [Required] public decimal CreditedQuantity { get; set; }
        [Required] public decimal LineExtension { get; set; }
        [Required] public TaxTotal Tax { get; set; } = new();
        [Required] public string Description { get; set; } = "";
        public decimal PriceAmount { get; set; }
    }
    #endregion

    #region UBL builder
    internal sealed class UblCreditNoteBuilder
    {
        private readonly XNamespace ubl = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private readonly XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private readonly XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        private XElement Money(string tag, decimal v)
            => new XElement(cbc + tag, new XAttribute("currencyID", "MYR"), v);
        private static XElement E(XName n, object v) => new(n, v);

        public XDocument Build(CreditNoteModel m)
        {
            var root = new XElement(ubl + "Invoice",   // credit-note sample still used <Invoice>
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),

                /* header */
                E(cbc + "ID", m.Id),
                E(cbc + "IssueDate", m.IssueDate.ToString("yyyy-MM-dd")),
                E(cbc + "IssueTime", m.IssueTime.ToString(@"hh\:mm\:ss") + "Z"),
                new XElement(cbc + "InvoiceTypeCode",
                    new XAttribute("listVersionID", m.TypeCodeVer),
                    m.InvoiceTypeCode),
                E(cbc + "DocumentCurrencyCode", m.CurrencyCode),
                E(cbc + "TaxCurrencyCode", m.TaxCurrencyCode),

                /* references */
                from br in m.BillingRefs
                select new XElement(cac + "BillingReference",
                    new XElement(cac + "InvoiceDocumentReference", E(cbc + "ID", br.Id))),
                from ad in m.AdditionalDocs select BuildDoc(ad),

                /* parties */
                BuildParty("AccountingSupplierParty", m.Supplier),
                BuildParty("AccountingCustomerParty", m.Customer),

                /* totals */
                BuildTaxTotal(m.TaxTotal),
                BuildMonetaryTotal(m.MonetaryTotal),

                /* lines */
                from ln in m.Lines select BuildLine(ln));

            return new XDocument(root);
        }

        private XElement BuildDoc(AdditionalDoc d) =>
            new XElement(cac + "AdditionalDocumentReference",
                E(cbc + "ID", d.Id),
                d.DocumentType == null ? null : E(cbc + "DocumentType", d.DocumentType),
                d.Description == null ? null : E(cbc + "DocumentDescription", d.Description));

        private XElement BuildParty(string tag, Party p)
        {
            var party = new XElement(cac + "Party",
                from id in p.Identifications
                select new XElement(cac + "PartyIdentification",
                    new XElement(cbc + "ID",
                        new XAttribute("schemeID", id.Scheme), id.Value)),
                BuildAddr(p.Address),
                new XElement(cac + "PartyLegalEntity",
                    E(cbc + "RegistrationName", p.Legal.RegistrationName),
                    p.Legal.CompanyId == null ? null : E(cbc + "CompanyID", p.Legal.CompanyId)),
                p.Contact == null ? null :
                    new XElement(cac + "Contact",
                        E(cbc + "Telephone", p.Contact.Telephone),
                        E(cbc + "ElectronicMail", p.Contact.Email)));

            return new XElement(cac + tag, party);
        }

        private XElement? BuildAddr(Address? a) => a == null ? null :
            new XElement(cac + "PostalAddress",
                E(cbc + "CityName", a.City),
                E(cbc + "PostalZone", a.Postal),
                E(cbc + "CountrySubentityCode", a.State),
                new XElement(cac + "AddressLine", E(cbc + "Line", a.Line)),
                new XElement(cac + "Country", E(cbc + "IdentificationCode", a.Country)));

        private XElement BuildTaxTotal(TaxTotal t) =>
            new XElement(cac + "TaxTotal",
                Money("TaxAmount", t.TaxAmount),
                new XElement(cac + "TaxSubtotal",
                    Money("TaxableAmount", t.TaxableAmount),
                    Money("TaxAmount", t.TaxAmount),
                    new XElement(cac + "TaxCategory",
                        E(cbc + "ID", t.TaxCategoryId),
                        new XElement(cac + "TaxScheme",
                            new XElement(cbc + "ID",
                                new XAttribute("schemeID", "UN/ECE 5153"),
                                new XAttribute("schemeAgencyID", "6"),
                                t.TaxSchemeId)))));

        private XElement BuildMonetaryTotal(MonetaryTotal m) =>
            new XElement(cac + "LegalMonetaryTotal",
                Money("LineExtensionAmount", m.LineExtensionAmount),
                Money("TaxExclusiveAmount", m.TaxExclusiveAmount),
                Money("TaxInclusiveAmount", m.TaxInclusiveAmount),
                Money("AllowanceTotalAmount", m.AllowanceTotalAmount),
                Money("ChargeTotalAmount", m.ChargeTotalAmount),
                Money("PayableAmount", m.PayableAmount));

        private XElement BuildLine(CreditLine l) =>
            new XElement(cac + "InvoiceLine",
                E(cbc + "ID", l.Id),
                new XElement(cbc + "CreditedQuantity",
                    new XAttribute("unitCode", "C62"), l.CreditedQuantity),
                Money("LineExtensionAmount", l.LineExtension),
                new XElement(cac + "TaxTotal",
                    Money("TaxAmount", l.Tax.TaxAmount),
                    new XElement(cac + "TaxSubtotal",
                        Money("TaxableAmount", l.Tax.TaxableAmount),
                        Money("TaxAmount", l.Tax.TaxAmount),
                        new XElement(cac + "TaxCategory",
                            E(cbc + "ID", l.Tax.TaxCategoryId),
                            new XElement(cac + "TaxScheme",
                                new XElement(cbc + "ID",
                                    new XAttribute("schemeID", "UN/ECE 5153"),
                                    new XAttribute("schemeAgencyID", "6"),
                                    l.Tax.TaxSchemeId))))),
                new XElement(cac + "Item", E(cbc + "Description", l.Description)),
                new XElement(cac + "Price", Money("PriceAmount", l.PriceAmount)));
    }
    #endregion
}

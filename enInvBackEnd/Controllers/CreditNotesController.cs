// Controllers/CreditNotesController.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        private readonly DocumentSubmissionService _submissionSvc;

        public CreditNotesController(IWebHostEnvironment env,
                                     DocumentSubmissionService submissionSvc)
        {
            _env = env;
            _submissionSvc = submissionSvc;
        }

        [HttpPost("submit/{company_id}")]
        public async Task<IActionResult> Submit(Guid company_id,
            [FromBody] CreditNoteModelCreditNote dto)
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
                await _submissionSvc.SubmitXmlAsync(path, dto.SupplierTaxId, company_id, "CreditNotes", dto.Id);
            var respBody = await resp.Content.ReadAsStringAsync();

            return Created(string.Empty, new { file, path, respBody });
        }
    }

    #region DTOs with MSIC and ItemPriceExtension

    public sealed class CreditNoteModelCreditNote
    {
        [Required] public string Id { get; set; } = "";
        [Required] public DateTime IssueDate { get; set; }
        public TimeSpan IssueTime { get; set; } = TimeSpan.Zero;
        public string InvoiceTypeCode { get; set; } = "02";
        public string TypeCodeVer { get; set; } = "1.0";
        [Required] public string CurrencyCode { get; set; } = "MYR";
        [Required] public string TaxCurrencyCode { get; set; } = "MYR";

        public List<BillingReferenceCreditNote> BillingRefs { get; set; } = new();
        public List<AdditionalDocCreditNote> AdditionalDocs { get; set; } = new();

        [Required] public PartyCreditNote Supplier { get; set; } = new();
        [Required] public PartyCreditNote Customer { get; set; } = new();

        [Required] public TaxTotalCreditNote TaxTotal { get; set; } = new();
        [Required] public MonetaryTotalCreditNote MonetaryTotal { get; set; } = new();

        [Required] public List<CreditLineCreditNote> Lines { get; set; } = new();

        [Required] public string SupplierTaxId { get; set; } = "";
    }

    public sealed class BillingReferenceCreditNote
    {
        public string Id { get; set; } = "";
    }

    public sealed class AdditionalDocCreditNote
    {
        [Required] public string Id { get; set; } = "";
        public string? DocumentType { get; set; }
        public string? Description { get; set; }
    }

    public sealed class PartyCreditNote
    {
        public List<PartyIdCreditNote> Identifications { get; set; } = new();
        public AddressCreditNote? Address { get; set; }
        public LegalEntityCreditNote Legal { get; set; } = new();

        // MSIC properties
        public string? IndustryCode { get; set; }
        public string? IndustryName { get; set; }

        public ContactCreditNote? Contact { get; set; }
    }

    public sealed class PartyIdCreditNote
    {
        public string Scheme { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public sealed class AddressCreditNote
    {
        public string City { get; set; } = "";
        public string Postal { get; set; } = "";
        public string State { get; set; } = "";
        public string Line { get; set; } = "";
        public string Country { get; set; } = "";
    }

    public sealed class LegalEntityCreditNote
    {
        [Required] public string RegistrationName { get; set; } = "";
        public string? CompanyId { get; set; }
    }

    public sealed class ContactCreditNote
    {
        public string Telephone { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public sealed class TaxTotalCreditNote
    {
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string TaxCategoryId { get; set; } = "01";
        public string TaxSchemeId { get; set; } = "OTH";
    }

    public sealed class MonetaryTotalCreditNote
    {
        public decimal LineExtensionAmount { get; set; }
        public decimal TaxExclusiveAmount { get; set; }
        public decimal TaxInclusiveAmount { get; set; }
        public decimal AllowanceTotalAmount { get; set; }
        public decimal ChargeTotalAmount { get; set; }
        public decimal PayableAmount { get; set; }
    }

    public sealed class CreditLineCreditNote
    {
        [Required] public string Id { get; set; } = "";
        [Required] public decimal CreditedQuantity { get; set; }
        [Required] public decimal LineExtension { get; set; }
        [Required] public TaxTotalCreditNote Tax { get; set; } = new();
        [Required] public string Description { get; set; } = "";

        public decimal PriceAmount { get; set; }
        public decimal ItemPriceExtensionAmount { get; set; }
        public string CommodityCodePtc { get; set; } = "";
        public string CommodityCodeClass { get; set; } = "";
    }

    #endregion

    #region UBL Credit Note XML Builder with fixes

    internal sealed class UblCreditNoteBuilder
    {
        private readonly XNamespace ubl = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private readonly XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private readonly XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        private XElement Money(string tag, decimal v)
            => new XElement(cbc + tag, new XAttribute("currencyID", "MYR"), v);

        private static XElement E(XName n, object v) => new(n, v);

        public XDocument Build(CreditNoteModelCreditNote m)
        {
            var root = new XElement(ubl + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),

                // header
                E(cbc + "ID", m.Id),
                E(cbc + "IssueDate", m.IssueDate.ToString("yyyy-MM-dd")),
                E(cbc + "IssueTime", m.IssueTime.ToString(@"hh\:mm\:ss") + "Z"),
                new XElement(cbc + "InvoiceTypeCode",
                    new XAttribute("listVersionID", m.TypeCodeVer),
                    m.InvoiceTypeCode),
                E(cbc + "DocumentCurrencyCode", m.CurrencyCode),
                E(cbc + "TaxCurrencyCode", m.TaxCurrencyCode),

                // references
                from br in m.BillingRefs
                select new XElement(cac + "BillingReference",
                    new XElement(cac + "InvoiceDocumentReference", E(cbc + "ID", br.Id))),

                from doc in m.AdditionalDocs select BuildDoc(doc),

                // parties
                BuildParty("AccountingSupplierParty", m.Supplier),
                BuildParty("AccountingCustomerParty", m.Customer),

                // totals
                BuildTaxTotal(m.TaxTotal),
                BuildMonetaryTotal(m.MonetaryTotal),

                // lines
                from ln in m.Lines select BuildLine(ln)
            );

            return new XDocument(root);
        }

        private XElement BuildDoc(AdditionalDocCreditNote d) =>
            new XElement(cac + "AdditionalDocumentReference",
                E(cbc + "ID", d.Id),
                d.DocumentType == null ? null : E(cbc + "DocumentType", d.DocumentType),
                d.Description == null ? null : E(cbc + "DocumentDescription", d.Description));

        private XElement BuildParty(string tag, PartyCreditNote p)
        {
            var party = new XElement(cac + "Party",
                // MSIC Industry Classification - REQUIRED
                !string.IsNullOrEmpty(p.IndustryCode) ?
                new XElement(cbc + "IndustryClassificationCode",
                    new XAttribute("name", p.IndustryName ?? ""), p.IndustryCode) : null,

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
                        E(cbc + "ElectronicMail", p.Contact.Email))
            );

            return new XElement(cac + tag, party);
        }

        private XElement? BuildAddr(AddressCreditNote? a) => a == null ? null :
            new XElement(cac + "PostalAddress",
                E(cbc + "CityName", a.City),
                E(cbc + "PostalZone", a.Postal),
                E(cbc + "CountrySubentityCode", a.State),
                new XElement(cac + "AddressLine", E(cbc + "Line", a.Line)),
                new XElement(cac + "Country", E(cbc + "IdentificationCode", a.Country)));

        private XElement BuildTaxTotal(TaxTotalCreditNote t) =>
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

        private XElement BuildMonetaryTotal(MonetaryTotalCreditNote m) =>
            new XElement(cac + "LegalMonetaryTotal",
                Money("LineExtensionAmount", m.LineExtensionAmount),
                Money("TaxExclusiveAmount", m.TaxExclusiveAmount),
                Money("TaxInclusiveAmount", m.TaxInclusiveAmount),
                Money("AllowanceTotalAmount", m.AllowanceTotalAmount),
                Money("ChargeTotalAmount", m.ChargeTotalAmount),
                Money("PayableAmount", m.PayableAmount));

        private XElement BuildLine(CreditLineCreditNote l) =>
            new XElement(cac + "InvoiceLine",
                E(cbc + "ID", l.Id),
                new XElement(cbc + "InvoicedQuantity",
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

                new XElement(cac + "Item",
                    E(cbc + "Description", l.Description),
                    new XElement(cac + "OriginCountry", E(cbc + "IdentificationCode", "MYS")),
                    new XElement(cac + "CommodityClassification",
                        new XElement(cbc + "ItemClassificationCode",
                            new XAttribute("listID", "PTC"), l.CommodityCodePtc)),
                    new XElement(cac + "CommodityClassification",
                        new XElement(cbc + "ItemClassificationCode",
                            new XAttribute("listID", "CLASS"), l.CommodityCodeClass))),

                new XElement(cac + "Price", Money("PriceAmount", l.PriceAmount)),

                new XElement(cac + "ItemPriceExtension",
                    Money("Amount", l.ItemPriceExtensionAmount))
            );
    }
    #endregion
}

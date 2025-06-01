// Controllers/ConsolidationController.cs
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
    public class ConsolidationController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly DocumentSubmissionService _submissionSvc;

        public ConsolidationController(IWebHostEnvironment env,
                                       DocumentSubmissionService submissionSvc)
        {
            _env = env;
            _submissionSvc = submissionSvc;
        }

        // POST: api/consolidation/submit/{company_id}
        [HttpPost("submit/{company_id}")]
        public async Task<IActionResult> Create(Guid company_id,
            [FromBody] ConsolidationInvoiceModel dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            /* ---------- build XML ---------- */
            var xmlDoc = new ConsolidationInvoiceBuilder().Build(dto);

            /* ---------- save ---------- */
            var outDir = Path.Combine(_env.ContentRootPath, "consolidations");
            Directory.CreateDirectory(outDir);

            var safeId = string.Concat((dto.Id ?? "Consolidation")
                              .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
            var fileName = $"{safeId}_{Guid.NewGuid():N}.xml";
            var fullPath = Path.Combine(outDir, fileName);

            await using (var fs = System.IO.File.Create(fullPath))
                xmlDoc.Save(fs);

            /* ---------- submit ---------- */
            HttpResponseMessage resp = await _submissionSvc.SubmitXmlAsync(fullPath, company_id, "Consolidation",dto.Id); // example tax-ID
            string respBody = await resp.Content.ReadAsStringAsync();

            return Created(string.Empty, new { fileName, fullPath, respBody });
        }
    }

    #region ───────── DTO / POCOs ─────────
    public sealed class ConsolidationInvoiceModel
    {
        [Required] public string Id { get; set; } = "";
        [Required] public DateTime IssueDate { get; set; }
        public TimeSpan IssueTime { get; set; } = TimeSpan.Zero;
        [Required] public string InvoiceTypeCode { get; set; } = "01";
        public string TypeCodeVer { get; set; } = "1.0";
        [Required] public string CurrencyCode { get; set; } = "MYR";
        [Required] public string TaxCurrencyCode { get; set; } = "MYR";

        [Required] public ConsolidationSupplierParty Supplier { get; set; } = new();

        [Required] public ConsolidationTaxTotal TaxTotal { get; set; } = new();
        [Required] public ConsolidationMonetaryTotal MonetaryTotal { get; set; } = new();

        [Required] public List<ConsolidationInvoiceLine> Lines { get; set; } = new();
    }

    public abstract class ConsolidationPartyBase
    {
        public string? AdditionalAccountID { get; set; }   // kept for backend use only
        [Required] public ConsolidationParty Party { get; set; } = new();
    }
    public sealed class ConsolidationSupplierParty : ConsolidationPartyBase { }

    public sealed class ConsolidationParty
    {
        public string? IndustryCode { get; set; }
        public string? IndustryName { get; set; }
        public List<ConsolidationPartyId> Identifications { get; set; } = new();
        public ConsolidationAddress? Address { get; set; }
        [Required] public ConsolidationLegalEntity LegalEntity { get; set; } = new();
        public ConsolidationContact? Contact { get; set; }
    }

    public sealed class ConsolidationPartyId
    {
        [Required] public string SchemeID { get; set; } = "";
        [Required] public string Value { get; set; } = "";
    }

    public sealed class ConsolidationAddress
    {
        public string? CityName { get; set; }
        public string? PostalZone { get; set; }
        public string? CountrySubentityCode { get; set; }
        public List<string>? Lines { get; set; }
        public string? CountryIdentificationCode { get; set; }
    }

    public sealed class ConsolidationLegalEntity
    {
        [Required] public string RegistrationName { get; set; } = "";
        public string? CompanyID { get; set; }
    }

    public sealed class ConsolidationContact
    {
        public string? Telephone { get; set; }
        public string? ElectronicMail { get; set; }
    }

    public sealed class ConsolidationTaxTotal
    {
        [Required] public decimal TaxAmount { get; set; }
        [Required] public decimal TaxableAmount { get; set; }
        [Required] public string TaxCategoryId { get; set; } = "01";
        [Required] public string TaxSchemeId { get; set; } = "OTH";
    }

    public sealed class ConsolidationMonetaryTotal
    {
        [Required] public decimal LineExtensionAmount { get; set; }
        [Required] public decimal TaxExclusiveAmount { get; set; }
        [Required] public decimal TaxInclusiveAmount { get; set; }
        public decimal AllowanceTotalAmount { get; set; }
        public decimal ChargeTotalAmount { get; set; }
        public decimal PayableRoundingAmount { get; set; }
        [Required] public decimal PayableAmount { get; set; }
    }

    public sealed class ConsolidationInvoiceLine
    {
        [Required] public string Id { get; set; } = "";
        [Required] public decimal Quantity { get; set; }
        [Required] public decimal LineExtensionAmount { get; set; }

        [Required] public ConsolidationTaxTotal TaxTotal { get; set; } = new();

        [Required] public string Description { get; set; } = "";

        public string? OriginCountryIdentificationCode { get; set; }
        public string? ItemClassificationCode { get; set; }

        [Required] public decimal PriceAmount { get; set; }
        [Required] public decimal ItemPriceExtensionAmount { get; set; }
    }
    #endregion

    #region ───────── XML Builder ─────────
    internal sealed class ConsolidationInvoiceBuilder
    {
        private readonly XNamespace ubl = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private readonly XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private readonly XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        private XElement Money(string tag, decimal v)
            => new XElement(cbc + tag, new XAttribute("currencyID", "MYR"), v);
        private static XElement E(XName n, object v) => new(n, v);

        public XDocument Build(ConsolidationInvoiceModel m)
        {
            var root = new XElement(ubl + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),

                /* header */
                E(cbc + "ID", m.Id),
                E(cbc + "IssueDate", m.IssueDate.ToString("yyyy-MM-dd")),
                E(cbc + "IssueTime", m.IssueTime.ToString(@"hh\:mm\:ss") + "Z"),
                new XElement(cbc + "InvoiceTypeCode",
                    new XAttribute("listVersionID", m.TypeCodeVer), m.InvoiceTypeCode),
                E(cbc + "DocumentCurrencyCode", m.CurrencyCode),
                E(cbc + "TaxCurrencyCode", m.TaxCurrencyCode),

                BuildSupplier(m.Supplier),
                BuildFixedCustomer(),

                BuildTaxTotal(m.TaxTotal),
                BuildMonetaryTotal(m.MonetaryTotal),

                from ln in m.Lines select BuildLine(ln));

            return new XDocument(root);
        }

        /* supplier without AdditionalAccountID */
        private XElement BuildSupplier(ConsolidationSupplierParty sp)
        {
            var p = new XElement(cac + "Party");

            if (!string.IsNullOrEmpty(sp.Party.IndustryCode))
                p.Add(new XElement(cbc + "IndustryClassificationCode",
                    new XAttribute("name", sp.Party.IndustryName ?? ""),
                    sp.Party.IndustryCode));

            foreach (var id in sp.Party.Identifications)
                p.Add(new XElement(cac + "PartyIdentification",
                    new XElement(cbc + "ID",
                        new XAttribute("schemeID", id.SchemeID), id.Value)));

            if (sp.Party.Address is { } a)
            {
                var addr = new XElement(cac + "PostalAddress");
                if (a.CityName != null) addr.Add(E(cbc + "CityName", a.CityName));
                if (a.PostalZone != null) addr.Add(E(cbc + "PostalZone", a.PostalZone));
                if (a.CountrySubentityCode != null) addr.Add(E(cbc + "CountrySubentityCode", a.CountrySubentityCode));
                if (a.Lines != null)
                    foreach (var ln in a.Lines)
                        addr.Add(new XElement(cac + "AddressLine", E(cbc + "Line", ln)));
                if (a.CountryIdentificationCode != null)
                    addr.Add(new XElement(cac + "Country",
                        E(cbc + "IdentificationCode", a.CountryIdentificationCode)));
                p.Add(addr);
            }

            p.Add(new XElement(cac + "PartyLegalEntity",
                E(cbc + "RegistrationName", sp.Party.LegalEntity.RegistrationName),
                sp.Party.LegalEntity.CompanyID == null ? null :
                    E(cbc + "CompanyID", sp.Party.LegalEntity.CompanyID)));

            if (sp.Party.Contact != null)
                p.Add(new XElement(cac + "Contact",
                    E(cbc + "Telephone", sp.Party.Contact.Telephone),
                    E(cbc + "ElectronicMail", sp.Party.Contact.ElectronicMail)));

            return new XElement(cac + "AccountingSupplierParty", p);
        }

        /* updated fixed customer */
        private XElement BuildFixedCustomer() =>
            new XElement(cac + "AccountingCustomerParty",
                new XElement(cac + "Party",
                    new XElement(cac + "PartyIdentification",
                        new XElement(cbc + "ID",
                            new XAttribute("schemeID", "TIN"), "EI00000000010")),
                    new XElement(cac + "PartyIdentification",
                        new XElement(cbc + "ID",
                            new XAttribute("schemeID", "BRN"), "NA")),
                    new XElement(cac + "PartyIdentification",
                        new XElement(cbc + "ID",
                            new XAttribute("schemeID", "SST"), "NA")),
                    new XElement(cac + "PartyIdentification",
                        new XElement(cbc + "ID",
                            new XAttribute("schemeID", "TTX"), "NA")),
                    new XElement(cac + "PostalAddress",
                        new XElement(cbc + "CityName"),
                        new XElement(cbc + "PostalZone"),
                        new XElement(cbc + "CountrySubentityCode"),
                        new XElement(cac + "AddressLine", new XElement(cbc + "Line", "NA")),
                        new XElement(cac + "AddressLine", new XElement(cbc + "Line")),
                        new XElement(cac + "AddressLine", new XElement(cbc + "Line")),
                        new XElement(cac + "Country",
                            new XElement(cbc + "IdentificationCode",
                                new XAttribute("listID", "ISO3166-1"),
                                new XAttribute("listAgencyID", "6")))),
                    new XElement(cac + "PartyLegalEntity",
                        new XElement(cbc + "RegistrationName", "Consolidated Buyers")),
                    new XElement(cac + "Contact",
                        new XElement(cbc + "Telephone", "NA"),
                        new XElement(cbc + "ElectronicMail", "NA"))));

        private XElement BuildTaxTotal(ConsolidationTaxTotal t) =>
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

        private XElement BuildMonetaryTotal(ConsolidationMonetaryTotal m) =>
            new XElement(cac + "LegalMonetaryTotal",
                Money("LineExtensionAmount", m.LineExtensionAmount),
                Money("TaxExclusiveAmount", m.TaxExclusiveAmount),
                Money("TaxInclusiveAmount", m.TaxInclusiveAmount),
                Money("AllowanceTotalAmount", m.AllowanceTotalAmount),
                Money("ChargeTotalAmount", m.ChargeTotalAmount),
                Money("PayableRoundingAmount", m.PayableRoundingAmount),
                Money("PayableAmount", m.PayableAmount));

        private XElement BuildLine(ConsolidationInvoiceLine l)
        {
            var pct = l.TaxTotal.TaxableAmount == 0
                ? 0
                : Math.Round(l.TaxTotal.TaxAmount / l.TaxTotal.TaxableAmount * 100, 2);

            return new XElement(cac + "InvoiceLine",
                E(cbc + "ID", l.Id),
                new XElement(cbc + "InvoicedQuantity",
                    new XAttribute("unitCode", "C62"), l.Quantity),
                Money("LineExtensionAmount", l.LineExtensionAmount),

                new XElement(cac + "TaxTotal",
                    Money("TaxAmount", l.TaxTotal.TaxAmount),
                    new XElement(cac + "TaxSubtotal",
                        Money("TaxableAmount", l.TaxTotal.TaxableAmount),
                        Money("TaxAmount", l.TaxTotal.TaxAmount),
                        E(cbc + "Percent", pct.ToString("0.00")),
                        new XElement(cac + "TaxCategory",
                            E(cbc + "ID", l.TaxTotal.TaxCategoryId),
                            new XElement(cac + "TaxScheme",
                                new XElement(cbc + "ID",
                                    new XAttribute("schemeID", "UN/ECE 5153"),
                                    new XAttribute("schemeAgencyID", "6"),
                                    l.TaxTotal.TaxSchemeId))))),

                new XElement(cac + "Item",
                    E(cbc + "Description", l.Description),
                    l.OriginCountryIdentificationCode == null ? null :
                        new XElement(cac + "OriginCountry",
                            E(cbc + "IdentificationCode", l.OriginCountryIdentificationCode)),
                    l.ItemClassificationCode == null ? null :
                        new XElement(cac + "CommodityClassification",
                            new XElement(cbc + "ItemClassificationCode",
                                new XAttribute("listID", "CLASS"),
                                l.ItemClassificationCode))),

                new XElement(cac + "Price", Money("PriceAmount", l.PriceAmount)),
                new XElement(cac + "ItemPriceExtension", Money("Amount", l.ItemPriceExtensionAmount)));
        }
    }
    #endregion
}

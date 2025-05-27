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

        public ConsolidationController(
            IWebHostEnvironment env,
            DocumentSubmissionService submissionSvc)
        {
            _env = env;
            _submissionSvc = submissionSvc;
        }

        // POST: api/consolidation/submit/{company_id}
        [HttpPost("submit/{company_id}")]
        public async Task<IActionResult> Create(
            Guid company_id,
            [FromBody] ConsolidationInvoiceModel dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var xmlDoc = new ConsolidationInvoiceBuilder().Build(dto);

            // save under {ContentRoot}/consolidations
            var outputDir = Path.Combine(_env.ContentRootPath, "consolidations");
            Directory.CreateDirectory(outputDir);

            var safeId = string.Concat((dto.Id ?? "Consolidation")
                .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
            var fileName = $"{safeId}_{Guid.NewGuid():N}.xml";
            var fullPath = Path.Combine(outputDir, fileName);

            await using (var fs = System.IO.File.Create(fullPath))
                xmlDoc.Save(fs);

            // submit to LHDN
           HttpResponseMessage resp = await _submissionSvc.SubmitXmlAsync(fullPath, dto.Supplier.AdditionalAccountID!, company_id);
            var respBody = await resp.Content.ReadAsStringAsync();

            return Created(string.Empty, new { fileName, fullPath, respBody});
        }
    }

    #region ────────── DTOs ──────────

    public sealed class ConsolidationInvoiceModel
    {
        [Required] public string Id { get; set; } = "";
        [Required] public DateTime IssueDate { get; set; }
        public TimeSpan IssueTime { get; set; } = TimeSpan.Zero;
        [Required] public string InvoiceTypeCode { get; set; } = "01";
        public string TypeCodeVer { get; set; } = "1.0";
        [Required] public string CurrencyCode { get; set; } = "MYR";
        [Required] public string TaxCurrencyCode { get; set; } = "MYR";

        [Required]
        public ConsolidationSupplierParty Supplier { get; set; }
            = new ConsolidationSupplierParty();

        [Required]
        public ConsolidationTaxTotal TaxTotal
        { get; set; } = new ConsolidationTaxTotal();

        [Required]
        public ConsolidationMonetaryTotal MonetaryTotal
        { get; set; } = new ConsolidationMonetaryTotal();

        [Required]
        public List<ConsolidationInvoiceLine> Lines
        { get; set; } = new List<ConsolidationInvoiceLine>();
    }

    public abstract class ConsolidationPartyBase
    {
        /// <summary>
        /// e.g. "CPT-CCN-W-211111-KL-000002"
        /// </summary>
        [Required] public string AdditionalAccountID { get; set; } = "";

        [Required]
        public ConsolidationParty Party { get; set; }
            = new ConsolidationParty();
    }
    public sealed class ConsolidationSupplierParty : ConsolidationPartyBase { }

    public sealed class ConsolidationParty
    {
        public string? IndustryCode { get; set; }
        public string? IndustryName { get; set; }

        public List<ConsolidationPartyId> Identifications
        { get; set; } = new List<ConsolidationPartyId>();

        public ConsolidationAddress? Address { get; set; }
        [Required]
        public ConsolidationLegalEntity LegalEntity
        { get; set; } = new ConsolidationLegalEntity();
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

        [Required]
        public ConsolidationTaxTotal TaxTotal
        { get; set; } = new ConsolidationTaxTotal();

        [Required] public string Description { get; set; } = "";

        public string? OriginCountryIdentificationCode { get; set; }
        public string? ItemClassificationCode { get; set; }

        [Required] public decimal PriceAmount { get; set; }
        [Required]
        public decimal ItemPriceExtensionAmount
        { get; set; }
    }
    #endregion

    #region ────────── Builder ──────────

    public sealed class ConsolidationInvoiceBuilder
    {
        private readonly XNamespace ubl = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private readonly XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private readonly XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        public XDocument Build(ConsolidationInvoiceModel m)
        {
            var invoice = new XElement(ubl + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),

                E("ID", m.Id),
                E("IssueDate", m.IssueDate.ToString("yyyy-MM-dd")),
                E("IssueTime", m.IssueTime.ToString(@"hh\:mm\:ss") + "Z"),
                new XElement(cbc + "InvoiceTypeCode",
                    new XAttribute("listVersionID", m.TypeCodeVer),
                    m.InvoiceTypeCode),
                E("DocumentCurrencyCode", m.CurrencyCode),
                E("TaxCurrencyCode", m.TaxCurrencyCode),

                BuildAccountingParty("AccountingSupplierParty", m.Supplier),

                // ---- fixed, unchanging consolidation customer ----
                new XElement(cac + "AccountingCustomerParty",
                  new XElement(cac + "Party",
                    new XElement(cac + "PartyIdentification",
                      new XElement(cbc + "ID",
                        new XAttribute("schemeID", "OTH"),
                        "Unregistered"
                      )
                    ),
                    new XElement(cac + "PostalAddress",
                      E("CityName", "NA"),
                      E("PostalZone", "NA"),
                      E("CountrySubentityCode", "NA"),
                      new XElement(cac + "AddressLine",
                        E("Line", "Consolidated Buyers")
                      ),
                      new XElement(cac + "Country",
                        E("IdentificationCode", "MYS")
                      )
                    ),
                    new XElement(cac + "PartyLegalEntity",
                      E("RegistrationName", "Consolidated Buyers")
                    ),
                    new XElement(cac + "Contact",
                      E("Telephone", "NA"),
                      E("ElectronicMail", "NA")
                    )
                  )
                ),

                BuildTaxTotal(m.TaxTotal),
                BuildMonetaryTotal(m.MonetaryTotal),

                from line in m.Lines
                select BuildInvoiceLine(line)
            );

            return new XDocument(invoice);
        }

        private XElement BuildAccountingParty(string tag, ConsolidationPartyBase pb)
        {
            var el = new XElement(cac + tag);

            el.Add(new XElement(cbc + "AdditionalAccountID",
                new XAttribute("schemeAgencyName", "CertEX"),
                pb.AdditionalAccountID));

            var p = new XElement(cac + "Party");

            if (!string.IsNullOrEmpty(pb.Party.IndustryCode))
                p.Add(new XElement(cbc + "IndustryClassificationCode",
                    new XAttribute("name", pb.Party.IndustryName ?? ""),
                    pb.Party.IndustryCode));

            foreach (var id in pb.Party.Identifications)
            {
                p.Add(new XElement(cac + "PartyIdentification",
                    new XElement(cbc + "ID",
                        new XAttribute("schemeID", id.SchemeID),
                        id.Value)));
            }

            if (pb.Party.Address != null)
            {
                var a = pb.Party.Address;
                var addr = new XElement(cac + "PostalAddress");
                if (!string.IsNullOrEmpty(a.CityName)) addr.Add(new XElement(cbc + "CityName", a.CityName));
                if (!string.IsNullOrEmpty(a.PostalZone)) addr.Add(new XElement(cbc + "PostalZone", a.PostalZone));
                if (!string.IsNullOrEmpty(a.CountrySubentityCode))
                    addr.Add(new XElement(cbc + "CountrySubentityCode", a.CountrySubentityCode));
                if (a.Lines != null)
                    foreach (var ln in a.Lines)
                        addr.Add(new XElement(cac + "AddressLine", E("Line", ln)));
                if (!string.IsNullOrEmpty(a.CountryIdentificationCode))
                    addr.Add(new XElement(cac + "Country",
                        E("IdentificationCode", a.CountryIdentificationCode)));
                p.Add(addr);
            }

            p.Add(new XElement(cac + "PartyLegalEntity",
                E("RegistrationName", pb.Party.LegalEntity.RegistrationName),
                pb.Party.LegalEntity.CompanyID is null ? null : E("CompanyID", pb.Party.LegalEntity.CompanyID)));

            if (pb.Party.Contact != null)
            {
                p.Add(new XElement(cac + "Contact",
                    E("Telephone", pb.Party.Contact.Telephone),
                    E("ElectronicMail", pb.Party.Contact.ElectronicMail)));
            }

            el.Add(p);
            return el;
        }

        private XElement BuildTaxTotal(ConsolidationTaxTotal t) =>
            new XElement(cac + "TaxTotal",
                E("TaxAmount", t.TaxAmount),
                new XElement(cac + "TaxSubtotal",
                    E("TaxableAmount", t.TaxableAmount),
                    E("TaxAmount", t.TaxAmount),
                    new XElement(cac + "TaxCategory",
                        E("ID", t.TaxCategoryId),
                        new XElement(cac + "TaxScheme",
                            new XElement(cbc + "ID",
                                new XAttribute("schemeID", "UN/ECE 5153"),
                                new XAttribute("schemeAgencyID", "6"),
                                t.TaxSchemeId
                            )
                        )
                    )
                )
            );

        private XElement BuildMonetaryTotal(ConsolidationMonetaryTotal m) =>
            new XElement(cac + "LegalMonetaryTotal",
                E("LineExtensionAmount", m.LineExtensionAmount),
                E("TaxExclusiveAmount", m.TaxExclusiveAmount),
                E("TaxInclusiveAmount", m.TaxInclusiveAmount),
                E("AllowanceTotalAmount", m.AllowanceTotalAmount),
                E("ChargeTotalAmount", m.ChargeTotalAmount),
                E("PayableRoundingAmount", m.PayableRoundingAmount),
                E("PayableAmount", m.PayableAmount)
            );

        private XElement BuildInvoiceLine(ConsolidationInvoiceLine l)
        {
            return new XElement(cac + "InvoiceLine",
                E("ID", l.Id),
                new XElement(cbc + "InvoicedQuantity",
                    new XAttribute("unitCode", "C62"), l.Quantity),
                new XElement(cbc + "LineExtensionAmount",
                    new XAttribute("currencyID", "MYR"), l.LineExtensionAmount),

                new XElement(cac + "TaxTotal",
                  E("TaxAmount", l.TaxTotal.TaxAmount),
                  new XElement(cac + "TaxSubtotal",
                    E("TaxableAmount", l.TaxTotal.TaxableAmount),
                    E("TaxAmount", l.TaxTotal.TaxAmount),
                    new XElement(cbc + "Percent",
                      (l.TaxTotal.TaxableAmount > 0
                       ? Math.Round(l.TaxTotal.TaxAmount / l.TaxTotal.TaxableAmount * 100, 2)
                       : 0
                      ).ToString("0.00")
                    ),
                    new XElement(cac + "TaxCategory",
                      E("ID", l.TaxTotal.TaxCategoryId),
                      new XElement(cac + "TaxScheme",
                        new XElement(cbc + "ID",
                          new XAttribute("schemeID", "UN/ECE 5153"),
                          new XAttribute("schemeAgencyID", "6"),
                          l.TaxTotal.TaxSchemeId
                        )
                      )
                    )
                  )
                ),

                new XElement(cac + "Item",
                  E("Description", l.Description),
                  l.OriginCountryIdentificationCode is null ? null :
                    new XElement(cac + "OriginCountry",
                      E("IdentificationCode", l.OriginCountryIdentificationCode)
                    ),
                  l.ItemClassificationCode is null ? null :
                    new XElement(cac + "CommodityClassification",
                      new XElement(cbc + "ItemClassificationCode",
                        new XAttribute("listID", "CLASS"),
                        l.ItemClassificationCode
                      )
                    )
                ),

                new XElement(cac + "Price",
                  E("PriceAmount", l.PriceAmount)
                ),
                new XElement(cac + "ItemPriceExtension",
                  new XElement(cbc + "Amount",
                    new XAttribute("currencyID", "MYR"),
                    l.ItemPriceExtensionAmount
                  )
                )
            );
        }

        private XElement E(string name, object content) =>
            new XElement(cbc + name, content);
    }
    #endregion
}

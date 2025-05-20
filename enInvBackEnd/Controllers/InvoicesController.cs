// Controllers/InvoicesController.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using enInvBackEnd.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace enInvBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly DocumentSubmissionService _submissionSvc;

        private readonly IWebHostEnvironment _env;

        public InvoicesController(IWebHostEnvironment env, DocumentSubmissionService submissionSvc)
        {
            _env = env;
            _submissionSvc = submissionSvc;
        }

        // POST: api/invoices
        [HttpPost("invoiceSubmit/{company_id}")]
        public async Task<IActionResult> Create(Guid company_id, [FromBody] InvoiceModel dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            /* ---------- Build UBL XML ---------- */
            var xmlDoc = new UblInvoiceBuilder().Build(dto);

            /* ---------- Save to {ContentRoot}\invoicess ---------- */
            // ContentRootPath === folder that contains Program.cs / appsettings.json
            string invoicesDir = Path.Combine(_env.ContentRootPath, "invoicess");

            // Create the directory tree if it doesn't exist (safe if it already does)
            Directory.CreateDirectory(invoicesDir);

            // Sanitize the ID for a file name

            string safeId = string.Concat((dto.Id ?? "Invoice").Where(c => !Path.GetInvalidFileNameChars().Contains(c)));

            string fileName = $"{safeId}_{Guid.NewGuid():N}.xml";   // :N format → 32-char hex without dashes

            string fullPath = Path.Combine(invoicesDir, fileName);

            // Write the XML to disk
            await using (var fs = System.IO.File.Create(fullPath))
            {
                xmlDoc.Save(fs);
            }

            // inside Create() – after you save fullPath


             HttpResponseMessage resp =await _submissionSvc.SubmitXmlAsync(fullPath, "142250926443",company_id);
             string respBody = await resp.Content.ReadAsStringAsync();

            /* ---------- Return 201 Created ---------- */
            // Not a public URL, but the absolute path on server
            return Created(string.Empty, new { fileName, fullPath,respBody });
        }

    }

    #region ─────────────── DTO / POCO layer ───────────────

    public sealed class InvoiceModel
    {
        [Required] public string Id { get; set; } = "";
        [Required] public DateTime IssueDate { get; set; }
        public TimeSpan IssueTime { get; set; } = TimeSpan.Zero;
        public string InvoiceTypeCode { get; set; } = "01";
        public string TypeCodeVer { get; set; } = "1.0";
        [Required] public string CurrencyCode { get; set; } = "MYR";
        [Required] public string TaxCurrencyCode { get; set; } = "MYR";

        public InvoicePeriod? InvoicePeriod { get; set; }
        public string? BillingReferenceId { get; set; }
        public List<AdditionalDoc> AdditionalDocs { get; set; } = new();

        [Required] public SupplierParty Supplier { get; set; } = new();
        [Required] public CustomerParty Customer { get; set; } = new();

        public PaymentMeans? PaymentMeans { get; set; }
        public string? PaymentTermsNote { get; set; }
        public List<PrepaidPayment> PrepaidPayments { get; set; } = new();
        public List<AllowanceCharge> HeaderCharges { get; set; } = new();

        [Required] public TaxTotal TaxTotal { get; set; } = new();
        [Required] public MonetaryTotal MonetaryTotal { get; set; } = new();
        [Required] public List<InvoiceLine> Lines { get; set; } = new();
    }

    public sealed class InvoicePeriod
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Description { get; set; }
    }

    public sealed class AdditionalDoc
    {
        [Required] public string Id { get; set; } = "";
        public string? DocumentType { get; set; }
        public string? Description { get; set; }
    }

    public abstract class PartyBase
    {
        public string? AdditionalAccountId { get; set; }
        [Required] public Party Party { get; set; } = new();
    }

    public sealed class SupplierParty : PartyBase { }
    public sealed class CustomerParty : PartyBase { }

    public sealed class Party
    {
        /* industry code optional */
        public string? IndustryCode { get; set; }
        public string? IndustryName { get; set; }

        public List<PartyId> Identifications { get; set; } = new();
        public Address? Address { get; set; }
        [Required] public LegalEntity LegalEntity { get; set; } = new();
        public Contact? Contact { get; set; }
    }

    public sealed class PartyId
    {
        public string Scheme { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public sealed class Address
    {
        public string City { get; set; } = "";
        public string Postal { get; set; } = "";
        public string State { get; set; } = "";
        public string Line { get; set; } = "";
        public string Country { get; set; } = "";
    }

    public sealed class LegalEntity
    {
        [Required] public string RegistrationName { get; set; } = "";
        public string? CompanyId { get; set; }
    }

    public sealed class Contact
    {
        public string Telephone { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public sealed class PaymentMeans
    {
        public string PaymentMeansCode { get; set; } = "";
        public string PayeeAccountId { get; set; } = "";
    }

    public sealed class PrepaidPayment
    {
        public string Id { get; set; } = "";
        public decimal PaidAmount { get; set; }
        public DateTime PaidDate { get; set; }
        public TimeSpan PaidTime { get; set; }
    }

    public sealed class AllowanceCharge
    {
        public bool ChargeIndicator { get; set; }
        public string Reason { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal? MultiplierFactor { get; set; }
    }

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
        public decimal PayableRoundingAmount { get; set; }
        public decimal PayableAmount { get; set; }
    }

    public sealed class InvoiceLine
    {
        [Required] public string Id { get; set; } = "";
        [Required] public decimal Quantity { get; set; }
        public string UnitCode { get; set; } = "C62";
        [Required] public decimal LineExtension { get; set; }

        public List<AllowanceCharge> Allowances { get; set; } = new();

        /* line-level tax */
        public TaxTotal Tax { get; set; } = new();

        [Required] public string Description { get; set; } = "";
        public string OriginCountryCode { get; set; } = "MYS";
        public string CommodityCodePtc { get; set; } = "";
        public string CommodityCodeClass { get; set; } = "";

        public decimal PriceAmount { get; set; }
        public decimal ItemPriceExtension { get; set; }
    }
    #endregion

    #region ─────────────── UBL Invoice Builder ───────────────
    public sealed class UblInvoiceBuilder
    {
        private readonly XNamespace ubl = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private readonly XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private readonly XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        public XDocument Build(InvoiceModel m)
        {
            var root = new XElement(ubl + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),

                /* ---------- header ---------- */
                E("ID", m.Id),
                E("IssueDate", m.IssueDate.ToString("yyyy-MM-dd")),
                E("IssueTime", m.IssueTime.ToString(@"hh\:mm\:ss") + "Z"),
                new XElement(cbc + "InvoiceTypeCode",
                    new XAttribute("listVersionID", m.TypeCodeVer),
                    m.InvoiceTypeCode),
                E("DocumentCurrencyCode", m.CurrencyCode),
                E("TaxCurrencyCode", m.TaxCurrencyCode),

                /* period */
                m.InvoicePeriod is null ? null :
                new XElement(cac + "InvoicePeriod",
                    E("StartDate", m.InvoicePeriod.StartDate.ToString("yyyy-MM-dd")),
                    E("EndDate", m.InvoicePeriod.EndDate.ToString("yyyy-MM-dd")),
                    m.InvoicePeriod.Description is null ? null :
                        E("Description", m.InvoicePeriod.Description)),

                /* billing reference */
                m.BillingReferenceId is null ? null :
                new XElement(cac + "BillingReference",
                    new XElement(cac + "AdditionalDocumentReference", E("ID", m.BillingReferenceId))),

                /* additional docs */
                from doc in m.AdditionalDocs select BuildAdditionalDoc(doc),

                /* supplier + customer */
                BuildAccountingParty("AccountingSupplierParty", m.Supplier),
                BuildAccountingParty("AccountingCustomerParty", m.Customer),

                /* payment means / terms */
                BuildPaymentMeans(m.PaymentMeans),

                m.PaymentTermsNote is null ? null :
                    new XElement(cac + "PaymentTerms", E("Note", m.PaymentTermsNote)),

                from pp in m.PrepaidPayments select BuildPrepaidPayment(pp),

                /* header-level allowances/charges */
                from ac in m.HeaderCharges select BuildAllowanceCharge(ac),

                /* totals */
                BuildTaxTotal(m.TaxTotal),
                BuildMonetaryTotal(m.MonetaryTotal),

                /* lines */
                from l in m.Lines select BuildInvoiceLine(l)
            );
            return new XDocument(root);
        }

        /* ───────── helper builders ───────── */

        private XElement BuildAdditionalDoc(AdditionalDoc d) =>
            new XElement(cac + "AdditionalDocumentReference",
                E("ID", d.Id),
                d.DocumentType is null ? null : E("DocumentType", d.DocumentType),
                d.Description is null ? null : E("DocumentDescription", d.Description));

        private XElement BuildAccountingParty(string tag, PartyBase pb)
        {
            var p = pb.Party;
            return new XElement(cac + tag,
                pb.AdditionalAccountId is null ? null :
                    new XElement(cbc + "AdditionalAccountID",
                        new XAttribute("schemeAgencyName", "CertEX"), pb.AdditionalAccountId),
                new XElement(cac + "Party",

                    p.IndustryCode is null ? null :
                        new XElement(cbc + "IndustryClassificationCode",
                            new XAttribute("name", p.IndustryName ?? ""), p.IndustryCode),

                    from id in p.Identifications
                    select new XElement(cac + "PartyIdentification",
                        new XElement(cbc + "ID",
                            new XAttribute("schemeID", id.Scheme), id.Value)),

                    p.Address is null ? null : BuildAddress(p.Address),

                    new XElement(cac + "PartyLegalEntity",
                        E("RegistrationName", p.LegalEntity.RegistrationName),
                        p.LegalEntity.CompanyId is null ? null : E("CompanyID", p.LegalEntity.CompanyId)),

                    p.Contact is null ? null :
                        new XElement(cac + "Contact",
                            E("Telephone", p.Contact.Telephone),
                            E("ElectronicMail", p.Contact.Email))
                )
            );
        }

        private XElement BuildAddress(Address a) =>
            new XElement(cac + "PostalAddress",
                E("CityName", a.City),
                E("PostalZone", a.Postal),
                E("CountrySubentityCode", a.State),
                new XElement(cac + "AddressLine", E("Line", a.Line)),
                new XElement(cac + "Country", E("IdentificationCode", a.Country)));

        private XElement BuildPaymentMeans(PaymentMeans? pm) =>
            pm is null ? null! :
            new XElement(cac + "PaymentMeans",
                E("PaymentMeansCode", pm.PaymentMeansCode),
                new XElement(cac + "PayeeFinancialAccount", E("ID", pm.PayeeAccountId)));

        private XElement BuildPrepaidPayment(PrepaidPayment p) =>
            new XElement(cac + "PrepaidPayment",
                E("ID", p.Id),
                Money("PaidAmount", p.PaidAmount),
                E("PaidDate", p.PaidDate.ToString("yyyy-MM-dd")),
                E("PaidTime", p.PaidTime.ToString(@"hh\:mm\:ss") + "Z"));

        private XElement BuildAllowanceCharge(AllowanceCharge a) =>
            new XElement(cac + "AllowanceCharge",
                E("ChargeIndicator", a.ChargeIndicator.ToString().ToLower()),
                E("AllowanceChargeReason", a.Reason),
                a.MultiplierFactor is null ? null : E("MultiplierFactorNumeric", a.MultiplierFactor.Value),
                Money("Amount", a.Amount));

        private XElement BuildTaxTotal(TaxTotal t) =>
            new XElement(cac + "TaxTotal",
                Money("TaxAmount", t.TaxAmount),
                new XElement(cac + "TaxSubtotal",
                    Money("TaxableAmount", t.TaxableAmount),
                    Money("TaxAmount", t.TaxAmount),
                    new XElement(cac + "TaxCategory",
                        E("ID", t.TaxCategoryId),
                        new XElement(cac + "TaxScheme",
                            new XElement(cbc + "ID",
                                new XAttribute("schemeID", "UN/ECE 5153"),
                                new XAttribute("schemeAgencyID", "6"), t.TaxSchemeId)))));

        private XElement BuildMonetaryTotal(MonetaryTotal m) =>
            new XElement(cac + "LegalMonetaryTotal",
                Money("LineExtensionAmount", m.LineExtensionAmount),
                Money("TaxExclusiveAmount", m.TaxExclusiveAmount),
                Money("TaxInclusiveAmount", m.TaxInclusiveAmount),
                Money("AllowanceTotalAmount", m.AllowanceTotalAmount),
                Money("ChargeTotalAmount", m.ChargeTotalAmount),
                Money("PayableRoundingAmount", m.PayableRoundingAmount),
                Money("PayableAmount", m.PayableAmount));

        private XElement BuildInvoiceLine(InvoiceLine l) =>
            new XElement(cac + "InvoiceLine",
                E("ID", l.Id),
                new XElement(cbc + "InvoicedQuantity",
                    new XAttribute("unitCode", l.UnitCode), l.Quantity),
                Money("LineExtensionAmount", l.LineExtension),

                from a in l.Allowances select BuildAllowanceCharge(a),

                BuildTaxTotal(l.Tax),

                new XElement(cac + "Item",
                    E("Description", l.Description),
                    new XElement(cac + "OriginCountry", E("IdentificationCode", l.OriginCountryCode)),
                    new XElement(cac + "CommodityClassification",
                        new XElement(cbc + "ItemClassificationCode",
                            new XAttribute("listID", "PTC"), l.CommodityCodePtc)),
                    new XElement(cac + "CommodityClassification",
                        new XElement(cbc + "ItemClassificationCode",
                            new XAttribute("listID", "CLASS"), l.CommodityCodeClass))),

                new XElement(cac + "Price", Money("PriceAmount", l.PriceAmount)),
                new XElement(cac + "ItemPriceExtension", Money("Amount", l.ItemPriceExtension)));
        /* ───── helpers ───── */
        private XElement Money(string tag, decimal amount) =>
            new XElement(cbc + tag, new XAttribute("currencyID", "MYR"), amount);

        private XElement E(string name, object? content) =>
            content is null ? null! : new XElement(cbc + name, content);
    }
    #endregion
}

// Controllers/SelfBilledInvoiceController.cs
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
    public class SelfBilledInvoiceController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly DocumentSubmissionService _submissionSvc;

        public SelfBilledInvoiceController(IWebHostEnvironment env,
                                           DocumentSubmissionService submissionSvc)
        {
            _env = env;
            _submissionSvc = submissionSvc;
        }

        [HttpPost("submit/{company_id}")]
        public async Task<IActionResult> Submit(Guid company_id,
            [FromBody] SelfBilledInvoiceModelSelfInvoice dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var xmlDoc = new SelfBilledInvoiceBuilderSelfInvoice().Build(dto);

            var outDir = Path.Combine(_env.ContentRootPath, "self-billed-invoices");
            Directory.CreateDirectory(outDir);

            var safeId = string.Concat((dto.Id ?? "SBI")
                           .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
            var fileName = $"{safeId}_{Guid.NewGuid():N}.xml";
            var fullPath = Path.Combine(outDir, fileName);

            await using (var fs = System.IO.File.Create(fullPath))
                xmlDoc.Save(fs);

            HttpResponseMessage resp = await _submissionSvc.SubmitXmlAsync(fullPath, dto.SupplierTaxId, company_id, "SelfBilledInvoice",dto.Id);
            string respBody = await resp.Content.ReadAsStringAsync();

            return Created(string.Empty, new { fileName, fullPath, respBody });
        }
    }

    #region DTOs for Self Billed Invoice (with SelfInvoice suffix)

    public sealed class SelfBilledInvoiceModelSelfInvoice
    {
        [Required] public string Id { get; set; } = "";
        [Required] public DateTime IssueDate { get; set; }
        public TimeSpan IssueTime { get; set; } = TimeSpan.Zero;
        [Required] public string InvoiceTypeCode { get; set; } = "11";  // Self-billed invoice code
        public string TypeCodeVer { get; set; } = "1.0";
        [Required] public string CurrencyCode { get; set; } = "MYR";
        [Required] public string TaxCurrencyCode { get; set; } = "MYR";

        public InvoicePeriodSelfInvoice? InvoicePeriod { get; set; }

        public List<BillingReferenceSelfInvoice> BillingReferences { get; set; } = new();
        public List<AdditionalDocumentReferenceSelfInvoice> AdditionalDocuments { get; set; } = new();

        [Required] public PartySelfInvoice Supplier { get; set; } = new();
        [Required] public PartySelfInvoice Customer { get; set; } = new();
        public PartySelfInvoice? DeliveryParty { get; set; }  // Optional delivery party

        public ShipmentSelfInvoice? Shipment { get; set; }  // Optional shipment info

        public PaymentMeansSelfInvoice? PaymentMeans { get; set; }
        public PaymentTermsSelfInvoice? PaymentTerms { get; set; }
        public List<PrepaidPaymentSelfInvoice> PrepaidPayments { get; set; } = new();

        public List<AllowanceChargeSelfInvoice> AllowanceCharges { get; set; } = new();

        [Required] public TaxTotalSelfInvoice TaxTotal { get; set; } = new();
        [Required] public MonetaryTotalSelfInvoice MonetaryTotal { get; set; } = new();

        [Required] public List<InvoiceLineSelfInvoice> Lines { get; set; } = new();

        [Required] public string SupplierTaxId { get; set; } = "";
    }

    public sealed class InvoicePeriodSelfInvoice
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Description { get; set; }
    }

    public sealed class BillingReferenceSelfInvoice
    {
        public AdditionalDocumentReferenceSelfInvoice AdditionalDocumentReference { get; set; } = new();
    }

    public sealed class AdditionalDocumentReferenceSelfInvoice
    {
        [Required] public string Id { get; set; } = "";
        public string? DocumentType { get; set; }
        public string? DocumentDescription { get; set; }
    }

    public sealed class PartySelfInvoice
    {
        public List<PartyIdSelfInvoice> Identifications { get; set; } = new();
        public AddressSelfInvoice? Address { get; set; }
        [Required] public LegalEntitySelfInvoice LegalEntity { get; set; } = new();
        public ContactSelfInvoice? Contact { get; set; }
    }

    public sealed class PartyIdSelfInvoice
    {
        public string SchemeId { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public sealed class AddressSelfInvoice
    {
        public string CityName { get; set; } = "";
        public string PostalZone { get; set; } = "";
        public string CountrySubentityCode { get; set; } = "";
        public List<string> AddressLines { get; set; } = new();
        public string CountryIdentificationCode { get; set; } = "";
    }

    public sealed class LegalEntitySelfInvoice
    {
        [Required] public string RegistrationName { get; set; } = "";
        public string? CompanyId { get; set; }
    }

    public sealed class ContactSelfInvoice
    {
        public string Telephone { get; set; } = "";
        public string ElectronicMail { get; set; } = "";
    }

    public sealed class ShipmentSelfInvoice
    {
        public string Id { get; set; } = "";
        public string HandlingCode { get; set; } = "";
        public string HandlingInstructions { get; set; } = "";
    }

    public sealed class PaymentMeansSelfInvoice
    {
        public string PaymentMeansCode { get; set; } = "";
        public string PayeeAccountId { get; set; } = "";
    }

    public sealed class PaymentTermsSelfInvoice
    {
        public string Note { get; set; } = "";
    }

    public sealed class PrepaidPaymentSelfInvoice
    {
        public string Id { get; set; } = "";
        public decimal PaidAmount { get; set; }
        public DateTime PaidDate { get; set; }
        public TimeSpan PaidTime { get; set; }
    }

    public sealed class AllowanceChargeSelfInvoice
    {
        public bool ChargeIndicator { get; set; }
        public string Reason { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal? MultiplierFactorNumeric { get; set; }
    }

    public sealed class TaxTotalSelfInvoice
    {
        public decimal TaxAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public string TaxCategoryId { get; set; } = "01";
        public string TaxSchemeId { get; set; } = "OTH";
        public string? TaxExemptionReason { get; set; }
    }

    public sealed class MonetaryTotalSelfInvoice
    {
        public decimal LineExtensionAmount { get; set; }
        public decimal TaxExclusiveAmount { get; set; }
        public decimal TaxInclusiveAmount { get; set; }
        public decimal AllowanceTotalAmount { get; set; }
        public decimal ChargeTotalAmount { get; set; }
        public decimal PayableRoundingAmount { get; set; }
        public decimal PayableAmount { get; set; }
    }

    public sealed class InvoiceLineSelfInvoice
    {
        [Required] public string Id { get; set; } = "";
        [Required] public decimal Quantity { get; set; }
        [Required] public decimal LineExtensionAmount { get; set; }
        [Required] public List<AllowanceChargeSelfInvoice> AllowanceCharges { get; set; } = new();
        [Required] public TaxTotalSelfInvoice TaxTotal { get; set; } = new();
        [Required] public string Description { get; set; } = "";
        public string OriginCountryIdentificationCode { get; set; } = "";
        public string CommodityCodePTC { get; set; } = "";
        public string CommodityCodeClass { get; set; } = "";
        public decimal PriceAmount { get; set; }
        public decimal ItemPriceExtensionAmount { get; set; }
    }

    #endregion

    #region XML Builder for Self Billed Invoice with suffix

    internal sealed class SelfBilledInvoiceBuilderSelfInvoice
    {
        private readonly XNamespace ubl = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private readonly XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private readonly XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        private XElement Money(string tag, decimal value) =>
            new XElement(cbc + tag, new XAttribute("currencyID", "MYR"), value);

        private XElement E(XName name, object? content) =>
            content == null ? null! : new XElement(name, content);

        public XDocument Build(SelfBilledInvoiceModelSelfInvoice m)
        {
            var root = new XElement(ubl + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),

                // Header
                E(cbc + "ID", m.Id),
                E(cbc + "IssueDate", m.IssueDate.ToString("yyyy-MM-dd")),
                E(cbc + "IssueTime", m.IssueTime.ToString(@"hh\:mm\:ss") + "Z"),
                new XElement(cbc + "InvoiceTypeCode",
                    new XAttribute("listVersionID", m.TypeCodeVer),
                    m.InvoiceTypeCode),
                E(cbc + "DocumentCurrencyCode", m.CurrencyCode),
                E(cbc + "TaxCurrencyCode", m.TaxCurrencyCode),

                // InvoicePeriod if any
                m.InvoicePeriod == null ? null :
                new XElement(cac + "InvoicePeriod",
                    E(cbc + "StartDate", m.InvoicePeriod.StartDate.ToString("yyyy-MM-dd")),
                    E(cbc + "EndDate", m.InvoicePeriod.EndDate.ToString("yyyy-MM-dd")),
                    m.InvoicePeriod.Description == null ? null : E(cbc + "Description", m.InvoicePeriod.Description)),

                // BillingReferences
                from br in m.BillingReferences
                select new XElement(cac + "BillingReference",
                    new XElement(cac + "AdditionalDocumentReference",
                        E(cbc + "ID", br.AdditionalDocumentReference.Id))),

                // AdditionalDocuments
                from doc in m.AdditionalDocuments
                select new XElement(cac + "AdditionalDocumentReference",
                    E(cbc + "ID", doc.Id),
                    doc.DocumentType == null ? null : E(cbc + "DocumentType", doc.DocumentType),
                    doc.DocumentDescription == null ? null : E(cbc + "DocumentDescription", doc.DocumentDescription)),

                // Parties
                BuildParty("AccountingSupplierParty", m.Supplier),
                BuildParty("AccountingCustomerParty", m.Customer),

                // Delivery (optional)
                m.DeliveryParty == null ? null :
                new XElement(cac + "Delivery",
                    new XElement(cac + "DeliveryParty", BuildPartyElement(m.DeliveryParty)),

                    m.Shipment == null ? null :
                    new XElement(cac + "Shipment",
                        E(cbc + "ID", m.Shipment.Id),
                        E(cbc + "HandlingCode", m.Shipment.HandlingCode),
                        E(cbc + "HandlingInstructions", m.Shipment.HandlingInstructions))),

                // PaymentMeans (optional)
                m.PaymentMeans == null ? null :
                new XElement(cac + "PaymentMeans",
                    E(cbc + "PaymentMeansCode", m.PaymentMeans.PaymentMeansCode),
                    new XElement(cac + "PayeeFinancialAccount",
                        E(cbc + "ID", m.PaymentMeans.PayeeAccountId))),

                // PaymentTerms (optional)
                m.PaymentTerms == null ? null :
                new XElement(cac + "PaymentTerms",
                    E(cbc + "Note", m.PaymentTerms.Note)),

                // PrepaidPayments
                from pp in m.PrepaidPayments
                select new XElement(cac + "PrepaidPayment",
                    E(cbc + "ID", pp.Id),
                    Money("PaidAmount", pp.PaidAmount),
                    E(cbc + "PaidDate", pp.PaidDate.ToString("yyyy-MM-dd")),
                    E(cbc + "PaidTime", pp.PaidTime.ToString(@"hh\:mm\:ss") + "Z")),

                // AllowanceCharges (header-level)
                from ac in m.AllowanceCharges
                select new XElement(cac + "AllowanceCharge",
                    E(cbc + "ChargeIndicator", ac.ChargeIndicator.ToString().ToLower()),
                    E(cbc + "AllowanceChargeReason", ac.Reason),
                    ac.MultiplierFactorNumeric == null ? null :
                    E(cbc + "MultiplierFactorNumeric", ac.MultiplierFactorNumeric.Value),
                    Money("Amount", ac.Amount)),

                // Totals
                BuildTaxTotal(m.TaxTotal),
                BuildLegalMonetaryTotal(m.MonetaryTotal),

                // Invoice lines
                from line in m.Lines select BuildInvoiceLine(line)
            );

            return new XDocument(root);
        }

        private XElement BuildParty(string tag, PartySelfInvoice p) =>
            new XElement(cac + tag, BuildPartyElement(p));

        private XElement BuildPartyElement(PartySelfInvoice p)
        {
            return new XElement(cac + "Party",
                from id in p.Identifications
                select new XElement(cac + "PartyIdentification",
                    new XElement(cbc + "ID",
                        new XAttribute("schemeID", id.SchemeId), id.Value)),

                p.Address == null ? null : BuildAddress(p.Address),

                new XElement(cac + "PartyLegalEntity",
                    E(cbc + "RegistrationName", p.LegalEntity.RegistrationName),
                    p.LegalEntity.CompanyId == null ? null : E(cbc + "CompanyID", p.LegalEntity.CompanyId)),

                p.Contact == null ? null :
                new XElement(cac + "Contact",
                    E(cbc + "Telephone", p.Contact.Telephone),
                    E(cbc + "ElectronicMail", p.Contact.ElectronicMail))
            );
        }

        private XElement BuildAddress(AddressSelfInvoice a) =>
            new XElement(cac + "PostalAddress",
                E(cbc + "CityName", a.CityName),
                E(cbc + "PostalZone", a.PostalZone),
                E(cbc + "CountrySubentityCode", a.CountrySubentityCode),
                from line in a.AddressLines
                select new XElement(cac + "AddressLine", E(cbc + "Line", line)),
                new XElement(cac + "Country", E(cbc + "IdentificationCode", a.CountryIdentificationCode)));

        private XElement BuildTaxTotal(TaxTotalSelfInvoice t) =>
            new XElement(cac + "TaxTotal",
                Money("TaxAmount", t.TaxAmount),
                new XElement(cac + "TaxSubtotal",
                    Money("TaxableAmount", t.TaxableAmount),
                    Money("TaxAmount", t.TaxAmount),
                    t.TaxExemptionReason == null ? null : E("TaxExemptionReason", t.TaxExemptionReason),
                    new XElement(cac + "TaxCategory",
                        E(cbc + "ID", t.TaxCategoryId),
                        new XElement(cac + "TaxScheme",
                            new XElement(cbc + "ID",
                                new XAttribute("schemeID", "UN/ECE 5153"),
                                new XAttribute("schemeAgencyID", "6"),
                                t.TaxSchemeId)))));

        private XElement BuildLegalMonetaryTotal(MonetaryTotalSelfInvoice m) =>
            new XElement(cac + "LegalMonetaryTotal",
                Money("LineExtensionAmount", m.LineExtensionAmount),
                Money("TaxExclusiveAmount", m.TaxExclusiveAmount),
                Money("TaxInclusiveAmount", m.TaxInclusiveAmount),
                Money("AllowanceTotalAmount", m.AllowanceTotalAmount),
                Money("ChargeTotalAmount", m.ChargeTotalAmount),
                Money("PayableRoundingAmount", m.PayableRoundingAmount),
                Money("PayableAmount", m.PayableAmount));

        private XElement BuildInvoiceLine(InvoiceLineSelfInvoice l)
        {
            return new XElement(cac + "InvoiceLine",
                E(cbc + "ID", l.Id),
                new XElement(cbc + "InvoicedQuantity",
                    new XAttribute("unitCode", "C62"), l.Quantity),
                Money("LineExtensionAmount", l.LineExtensionAmount),

                from ac in l.AllowanceCharges
                select new XElement(cac + "AllowanceCharge",
                    E(cbc + "ChargeIndicator", ac.ChargeIndicator.ToString().ToLower()),
                    E(cbc + "AllowanceChargeReason", ac.Reason),
                    ac.MultiplierFactorNumeric == null ? null :
                        E(cbc + "MultiplierFactorNumeric", ac.MultiplierFactorNumeric.Value),
                    Money("Amount", ac.Amount)),

                new XElement(cac + "TaxTotal",
                    Money("TaxAmount", l.TaxTotal.TaxAmount),
                    new XElement(cac + "TaxSubtotal",
                        Money("TaxableAmount", l.TaxTotal.TaxableAmount),
                        Money("TaxAmount", l.TaxTotal.TaxAmount),
                        l.TaxTotal.TaxExemptionReason == null ? null : E("TaxExemptionReason", l.TaxTotal.TaxExemptionReason),
                        new XElement(cac + "TaxCategory",
                            E(cbc + "ID", l.TaxTotal.TaxCategoryId),
                            new XElement(cac + "TaxScheme",
                                new XElement(cbc + "ID",
                                    new XAttribute("schemeID", "UN/ECE 5153"),
                                    new XAttribute("schemeAgencyID", "6"),
                                    l.TaxTotal.TaxSchemeId))))),

                new XElement(cac + "Item",
                    E(cbc + "Description", l.Description),
                    new XElement(cac + "OriginCountry",
                        E(cbc + "IdentificationCode", l.OriginCountryIdentificationCode)),
                    new XElement(cac + "CommodityClassification",
                        new XElement(cbc + "ItemClassificationCode",
                            new XAttribute("listID", "PTC"), l.CommodityCodePTC)),
                    new XElement(cac + "CommodityClassification",
                        new XElement(cbc + "ItemClassificationCode",
                            new XAttribute("listID", "CLASS"), l.CommodityCodeClass))),

                new XElement(cac + "Price",
                    Money("PriceAmount", l.PriceAmount)),
                new XElement(cac + "ItemPriceExtension",
                    Money("Amount", l.ItemPriceExtensionAmount))
            );
        }
    }
    #endregion
}

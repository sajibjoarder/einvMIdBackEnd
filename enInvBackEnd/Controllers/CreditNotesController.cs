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
    public sealed class CreditNotesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly DocumentSubmissionService _submissionSvc;

        public CreditNotesController(IWebHostEnvironment env, DocumentSubmissionService submissionSvc)
        {
            _env = env;
            _submissionSvc = submissionSvc;
        }

        // POST: api/creditnotes/creditNoteSubmit/{company_id}
        [HttpPost("creditNoteSubmit/{company_id}")]
        public async Task<IActionResult> Create(Guid company_id,
            [FromBody] CreditNoteModel dto)
        {
            // Only check for TaxExemptionReason when needed (CF366)
            foreach (var ln in dto.Lines)
            {
                bool isExempt = ln.Tax.TaxAmount == 0 &&
                                ln.Tax.TaxCategoryId?.Equals("E",
                                        StringComparison.OrdinalIgnoreCase) == true;
                if (isExempt && string.IsNullOrWhiteSpace(ln.Tax.TaxExemptionReason))
                    ModelState.AddModelError("TaxExemptionReason",
                        $"Line {ln.Id}: TaxExemptionReason is required when tax is exempt.");
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            /* ----- build XML identical to LHDN sample + fixes ----- */
            var xml = new UblCreditNoteBuilder().Build(dto);

            /* ----- save file ----- */
            string dir = Path.Combine(_env.ContentRootPath, "creditnotes");
            Directory.CreateDirectory(dir);

            string safeId = string.Concat(dto.Id.Where(c =>
                              !Path.GetInvalidFileNameChars().Contains(c)));
            string fileName = $"{safeId}_{Guid.NewGuid():N}.xml";
            string fullPath = Path.Combine(dir, fileName);

            await using (var fs = System.IO.File.Create(fullPath))
                xml.Save(fs);

            /* ----- submit to IRBM / LHDN ----- */
            var CreaditNoteID = Guid.NewGuid(); // Generate a new GUID for the invoice ID in db
            HttpResponseMessage resp = await _submissionSvc.SubmitXmlAsync(fullPath,company_id,"CreditNote",dto.Id, CreaditNoteID);

            string respBody = await resp.Content.ReadAsStringAsync();
            return Created(string.Empty, new { fileName, fullPath, respBody,CreaditNoteID });
        }
    }

    // DTO / POCO layer (CN_*)
    public sealed class CreditNoteModel
    {
        [Required] public string Id { get; set; } = "";
        [Required] public DateTime IssueDate { get; set; }
        [Required] public TimeSpan IssueTime { get; set; }
        public string InvoiceTypeCode { get; set; } = "02";
        public string TypeCodeVer { get; set; } = "1.0";
        [Required] public string CurrencyCode { get; set; } = "MYR";
        [Required] public string TaxCurrencyCode { get; set; } = "MYR";

        public CN_InvoicePeriod? InvoicePeriod { get; set; }
        public List<CN_BillingReference> BillingReferences { get; set; } = new();
        public List<CN_AdditionalDoc> AdditionalDocs { get; set; } = new();

        [Required] public CN_SupplierParty Supplier { get; set; } = new();
        [Required] public CN_CustomerParty Customer { get; set; } = new();

        public CN_DeliveryInfo? Delivery { get; set; }
        public CN_PaymentMeans? PaymentMeans { get; set; }
        public string? PaymentTermsNote { get; set; }
        public List<CN_PrepaidPayment> PrepaidPayments { get; set; } = new();

        public List<CN_AllowanceCharge> HeaderCharges { get; set; } = new();

        [Required] public CN_TaxTotal TaxTotal { get; set; } = new();
        [Required] public CN_MonetaryTotal MonetaryTotal { get; set; } = new();

        [Required] public List<CN_CreditNoteLine> Lines { get; set; } = new();
    }

    public sealed class CN_InvoicePeriod
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Description { get; set; }
    }

    public sealed class CN_BillingReference
    {
        public string? InvoiceId { get; set; }
        public string? InvoiceUuid { get; set; }
        public string? AdditionalDocumentId { get; set; }
    }

    public sealed class CN_AdditionalDoc
    {
        [Required] public string Id { get; set; } = "";
        public string? DocumentType { get; set; }
        public string? Description { get; set; }
    }

    public abstract class CN_PartyBase
    {
        public string? AdditionalAccountId { get; set; }
        [Required] public CN_Party Party { get; set; } = new();
    }

    public sealed class CN_SupplierParty : CN_PartyBase { }
    public sealed class CN_CustomerParty : CN_PartyBase { }
    public sealed class CN_DeliveryParty : CN_PartyBase { }

    public sealed class CN_Party
    {
        public string? IndustryCode { get; set; }
        public string? IndustryName { get; set; }
        public List<CN_PartyId> Identifications { get; set; } = new();
        public CN_Address? Address { get; set; }
        [Required] public CN_LegalEntity LegalEntity { get; set; } = new();
        public CN_Contact? Contact { get; set; }
    }

    public sealed class CN_PartyId
    {
        public string Scheme { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public sealed class CN_Address
    {
        public string City { get; set; } = "";
        public string Postal { get; set; } = "";
        public string State { get; set; } = "";
        public List<string> Lines { get; set; } = new();
        public string Country { get; set; } = "";
    }

    public sealed class CN_LegalEntity
    {
        [Required] public string RegistrationName { get; set; } = "";
    }

    public sealed class CN_Contact
    {
        public string Telephone { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public sealed class CN_DeliveryInfo
    {
        [Required] public CN_DeliveryParty DeliveryParty { get; set; } = new();
        public CN_Shipment? Shipment { get; set; }
    }

    public sealed class CN_Shipment
    {
        public string Id { get; set; } = "";
        public List<CN_FreightAllowanceCharge> FreightCharges { get; set; } = new();
    }

    public sealed class CN_FreightAllowanceCharge
    {
        public bool ChargeIndicator { get; set; }
        public string Reason { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public sealed class CN_PaymentMeans
    {
        public string PaymentMeansCode { get; set; } = "";
        public string PayeeAccountId { get; set; } = "";
    }

    public sealed class CN_PrepaidPayment
    {
        public string Id { get; set; } = "";
        public decimal PaidAmount { get; set; }
        public DateTime PaidDate { get; set; }
        public TimeSpan PaidTime { get; set; }
    }

    public sealed class CN_AllowanceCharge
    {
        public bool ChargeIndicator { get; set; }
        public string Reason { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal? MultiplierFactor { get; set; }
    }

    public sealed class CN_TaxTotal
    {
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string TaxCategoryId { get; set; } = "01";
        public string TaxSchemeId { get; set; } = "OTH";
        public string? TaxExemptionReason { get; set; } // for CF366
    }

    public sealed class CN_MonetaryTotal
    {
        public decimal LineExtensionAmount { get; set; }
        public decimal TaxExclusiveAmount { get; set; }
        public decimal TaxInclusiveAmount { get; set; }
        public decimal AllowanceTotalAmount { get; set; }
        public decimal ChargeTotalAmount { get; set; }
        public decimal PayableRoundingAmount { get; set; }
        public decimal PayableAmount { get; set; }
    }

    public sealed class CN_CreditNoteLine
    {
        [Required] public string Id { get; set; } = "";
        [Required] public decimal Quantity { get; set; }
        public string UnitCode { get; set; } = "C62";
        [Required] public decimal LineExtension { get; set; }

        public List<CN_AllowanceCharge> Allowances { get; set; } = new();
        public CN_TaxTotal Tax { get; set; } = new();

        [Required] public string Description { get; set; } = "";
        public string OriginCountryCode { get; set; } = "MYS";
        public string CommodityCodePtc { get; set; } = "";
        public string CommodityCodeClass { get; set; } = "";

        public decimal PriceAmount { get; set; }
        public decimal ItemPriceExtension { get; set; }
    }

    // UBL builder
    public sealed class UblCreditNoteBuilder
    {
        private readonly XNamespace ubl = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private readonly XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private readonly XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        public XDocument Build(CreditNoteModel m)
        {
            var root = new XElement(ubl + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),

                E("ID", m.Id),
                E("IssueDate", m.IssueDate.ToString("yyyy-MM-dd")),
                E("IssueTime", m.IssueTime.ToString(@"hh\:mm\:ss") + "Z"),
                new XElement(cbc + "InvoiceTypeCode",
                    new XAttribute("listVersionID", m.TypeCodeVer), m.InvoiceTypeCode),
                E("DocumentCurrencyCode", m.CurrencyCode),
                E("TaxCurrencyCode", m.TaxCurrencyCode),

                m.InvoicePeriod == null ? null :
                    new XElement(cac + "InvoicePeriod",
                        E("StartDate", m.InvoicePeriod.StartDate.ToString("yyyy-MM-dd")),
                        E("EndDate", m.InvoicePeriod.EndDate.ToString("yyyy-MM-dd")),
                        m.InvoicePeriod.Description == null ? null :
                            E("Description", m.InvoicePeriod.Description)),

                from br in m.BillingReferences select BuildBillingReference(br),
                from doc in m.AdditionalDocs
                select new XElement(cac + "AdditionalDocumentReference",
                        E("ID", doc.Id),
                        doc.DocumentType == null ? null : E("DocumentType", doc.DocumentType),
                        doc.Description == null ? null : E("DocumentDescription", doc.Description)),

                BuildAccountingParty("AccountingSupplierParty", m.Supplier),
                BuildAccountingParty("AccountingCustomerParty", m.Customer),

                m.Delivery == null ? null : BuildDelivery(m.Delivery),

                BuildPaymentMeans(m.PaymentMeans),
                m.PaymentTermsNote == null ? null :
                    new XElement(cac + "PaymentTerms", E("Note", m.PaymentTermsNote)),
                from pp in m.PrepaidPayments select BuildPrepaidPayment(pp),
                from ac in m.HeaderCharges select BuildAllowanceCharge(ac),

                BuildTaxTotal(m.TaxTotal),
                BuildMonetaryTotal(m.MonetaryTotal),

                from l in m.Lines select BuildLine(l)
            );

            return new XDocument(root);
        }

        private XElement BuildBillingReference(CN_BillingReference b) =>
            new XElement(cac + "BillingReference",
                b.InvoiceId == null ? null :
                    new XElement(cac + "InvoiceDocumentReference",
                        E("ID", b.InvoiceId),
                        b.InvoiceUuid == null ? null : E("UUID", b.InvoiceUuid)),
                b.AdditionalDocumentId == null ? null :
                    new XElement(cac + "AdditionalDocumentReference",
                        E("ID", b.AdditionalDocumentId)));

        private XElement BuildAccountingParty(string tag, CN_PartyBase pb)
        {
            var p = pb.Party;
            return new XElement(cac + tag,
                pb.AdditionalAccountId == null ? null :
                    new XElement(cbc + "AdditionalAccountID",
                        new XAttribute("schemeAgencyName", "CertEX"),
                        pb.AdditionalAccountId),
                new XElement(cac + "Party",
                    p.IndustryCode == null ? null :
                        new XElement(cbc + "IndustryClassificationCode",
                            new XAttribute("name", p.IndustryName ?? ""),
                            p.IndustryCode),
                    from id in p.Identifications
                    select new XElement(cac + "PartyIdentification",
                        new XElement(cbc + "ID",
                            new XAttribute("schemeID", id.Scheme), id.Value)),
                    p.Address == null ? null : BuildAddress(p.Address),
                    new XElement(cac + "PartyLegalEntity",
                        E("RegistrationName", p.LegalEntity.RegistrationName)),
                    p.Contact == null ? null :
                        new XElement(cac + "Contact",
                            E("Telephone", p.Contact.Telephone),
                            E("ElectronicMail", p.Contact.Email))
                ));
        }

        private XElement BuildAddress(CN_Address a) =>
            new XElement(cac + "PostalAddress",
                E("CityName", a.City),
                E("PostalZone", a.Postal),
                E("CountrySubentityCode", a.State),
                from ln in a.Lines
                select new XElement(cac + "AddressLine", E("Line", ln)),
                new XElement(cac + "Country", E("IdentificationCode", a.Country)));

        private XElement BuildDelivery(CN_DeliveryInfo d) =>
            new XElement(cac + "Delivery",
                BuildAccountingParty("DeliveryParty", d.DeliveryParty),
                d.Shipment == null ? null : BuildShipment(d.Shipment));

        private XElement BuildShipment(CN_Shipment s) =>
            new XElement(cac + "Shipment",
                E("ID", s.Id),
                from c in s.FreightCharges
                select new XElement(cac + "FreightAllowanceCharge",
                        E("ChargeIndicator", c.ChargeIndicator.ToString().ToLower()),
                        E("AllowanceChargeReason", c.Reason),
                        Money("Amount", c.Amount)));

        private XElement BuildPaymentMeans(CN_PaymentMeans? pm) =>
            pm == null ? null :
                new XElement(cac + "PaymentMeans",
                    E("PaymentMeansCode", pm.PaymentMeansCode),
                    new XElement(cac + "PayeeFinancialAccount",
                        E("ID", pm.PayeeAccountId)));

        private XElement BuildPrepaidPayment(CN_PrepaidPayment p) =>
            new XElement(cac + "PrepaidPayment",
                E("ID", p.Id),
                Money("PaidAmount", p.PaidAmount),
                E("PaidDate", p.PaidDate.ToString("yyyy-MM-dd")),
                E("PaidTime", p.PaidTime.ToString(@"hh\:mm\:ss") + "Z"));

        private XElement BuildAllowanceCharge(CN_AllowanceCharge a) =>
            new XElement(cac + "AllowanceCharge",
                E("ChargeIndicator", a.ChargeIndicator.ToString().ToLower()),
                E("AllowanceChargeReason", a.Reason),
                a.MultiplierFactor == null ? null :
                    E("MultiplierFactorNumeric", a.MultiplierFactor.Value),
                Money("Amount", a.Amount));

        private XElement BuildTaxTotal(CN_TaxTotal t) =>
            new XElement(cac + "TaxTotal",
                Money("TaxAmount", t.TaxAmount),
                new XElement(cac + "TaxSubtotal",
                    Money("TaxableAmount", t.TaxableAmount),
                    Money("TaxAmount", t.TaxAmount),
                    new XElement(cac + "TaxCategory",
                        E("ID", t.TaxCategoryId),
                        t.TaxExemptionReason == null ? null :
                            E("TaxExemptionReason", t.TaxExemptionReason),
                        new XElement(cac + "TaxScheme",
                            new XElement(cbc + "ID",
                                new XAttribute("schemeID", "UN/ECE 5153"),
                                new XAttribute("schemeAgencyID", "6"),
                                t.TaxSchemeId)))));

        private XElement BuildMonetaryTotal(CN_MonetaryTotal m) =>
            new XElement(cac + "LegalMonetaryTotal",
                Money("LineExtensionAmount", m.LineExtensionAmount),
                Money("TaxExclusiveAmount", m.TaxExclusiveAmount),
                Money("TaxInclusiveAmount", m.TaxInclusiveAmount),
                Money("AllowanceTotalAmount", m.AllowanceTotalAmount),
                Money("ChargeTotalAmount", m.ChargeTotalAmount),
                Money("PayableRoundingAmount", m.PayableRoundingAmount),
                Money("PayableAmount", m.PayableAmount));

        private XElement BuildLine(CN_CreditNoteLine l) =>
            new XElement(cac + "InvoiceLine",
                E("ID", l.Id),
                new XElement(cbc + "InvoicedQuantity",
                    new XAttribute("unitCode", l.UnitCode), l.Quantity),
                Money("LineExtensionAmount", l.LineExtension),
                from ac in l.Allowances select BuildAllowanceCharge(ac),
                BuildTaxTotal(l.Tax),
                new XElement(cac + "Item",
                    E("Description", l.Description),
                    new XElement(cac + "OriginCountry",
                        E("IdentificationCode", l.OriginCountryCode)),
                    new XElement(cac + "CommodityClassification",
                        new XElement(cbc + "ItemClassificationCode",
                            new XAttribute("listID", "PTC"), l.CommodityCodePtc)),
                    new XElement(cac + "CommodityClassification",
                        new XElement(cbc + "ItemClassificationCode",
                            new XAttribute("listID", "CLASS"), l.CommodityCodeClass))),
                new XElement(cac + "Price", Money("PriceAmount", l.PriceAmount)),
                new XElement(cac + "ItemPriceExtension", Money("Amount", l.ItemPriceExtension))
            );

        private XElement Money(string tag, decimal amount) =>
            new XElement(cbc + tag, new XAttribute("currencyID", "MYR"), amount);

        private XElement E(string name, object? value) =>
            value == null ? null! : new XElement(cbc + name, value);
    }
}

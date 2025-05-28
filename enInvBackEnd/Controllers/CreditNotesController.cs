using enInvBackEnd.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace enInvBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreditNotesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly DocumentSubmissionService _submissionSvc;

        public CreditNotesController(IWebHostEnvironment env, DocumentSubmissionService submissionSvc)
        {
            _env = env;
            _submissionSvc = submissionSvc;
        }

        [HttpPost("submit/{company_id}")]
        public async Task<IActionResult> Submit(Guid company_id, [FromBody] CreditNoteModels.CreditNoteModel_CN dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var xml = CreditNoteModels.UblCreditNoteBuilder_CN.Build(dto);

            var dir = Path.Combine(_env.ContentRootPath, "credit-notes");
            Directory.CreateDirectory(dir);

            var safeId = string.Concat((dto.ID ?? "CN").Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
            var fileName = $"{safeId}_{Guid.NewGuid():N}.xml";
            var filePath = Path.Combine(dir, fileName);

            await using (var fs = System.IO.File.Create(filePath))
                xml.Save(fs);

            HttpResponseMessage resp = await _submissionSvc.SubmitXmlAsync(filePath, dto.AccountingSupplierParty.AdditionalAccountID, company_id, "CreditNotes", dto.ID);
            var respBody = await resp.Content.ReadAsStringAsync();

            return Created(string.Empty, new { fileName, filePath, respBody });
        }
    }
}

namespace enInvBackEnd.Controllers.CreditNoteModels
{
    #region DTO Classes_CN

    public sealed class CreditNoteModel_CN
    {
        [Required] public string ID { get; set; } = "";
        [Required] public DateTime IssueDate { get; set; }
        [Required] public string IssueTime { get; set; } = "00:00:00Z";
        [Required] public string InvoiceTypeCode { get; set; } = "02";
        [Required] public string DocumentCurrencyCode { get; set; } = "MYR";
        [Required] public string TaxCurrencyCode { get; set; } = "MYR";

        public List<BillingReference_CN> BillingReferences { get; set; } = new();
        public List<AdditionalDocumentReference_CN> AdditionalDocumentReferences { get; set; } = new();

        [Required] public AccountingSupplierParty_CN AccountingSupplierParty { get; set; } = new();
        [Required] public AccountingCustomerParty_CN AccountingCustomerParty { get; set; } = new();

        public Delivery_CN? Delivery { get; set; }
        public PaymentMeans_CN? PaymentMeans { get; set; }
        public PaymentTerms_CN? PaymentTerms { get; set; }
        public List<PrepaidPayment_CN> PrepaidPayments { get; set; } = new();
        public List<AllowanceCharge_CN> AllowanceCharges { get; set; } = new();

        [Required] public TaxTotal_CN TaxTotal { get; set; } = new();
        [Required] public LegalMonetaryTotal_CN LegalMonetaryTotal { get; set; } = new();
        [Required] public List<InvoiceLine_CN> InvoiceLines { get; set; } = new();
    }

    public sealed class BillingReference_CN
    {
        [Required] public InvoiceDocumentReference_CN InvoiceDocumentReference { get; set; } = new();
        public AdditionalDocumentReference_CN? AdditionalDocumentReference { get; set; }
    }

    public sealed class InvoiceDocumentReference_CN
    {
        [Required] public string ID { get; set; } = "";
        public string? UUID { get; set; }
    }

    public sealed class AdditionalDocumentReference_CN
    {
        [Required] public string ID { get; set; } = "";
        public string? DocumentType { get; set; }
        public string? DocumentDescription { get; set; }
    }

    public sealed class AccountingSupplierParty_CN
    {
        [Required] public string AdditionalAccountID { get; set; } = "";
        [Required] public Party_CN Party { get; set; } = new();
    }

    public sealed class AccountingCustomerParty_CN
    {
        [Required] public Party_CN Party { get; set; } = new();
    }

    public sealed class Party_CN
    {
        public List<PartyIdentification_CN> PartyIdentifications { get; set; } = new();
        [Required] public PostalAddress_CN PostalAddress { get; set; } = new();
        [Required] public PartyLegalEntity_CN PartyLegalEntity { get; set; } = new();
        [Required] public Contact_CN Contact { get; set; } = new();
        public IndustryClassificationCode_CN? IndustryClassificationCode { get; set; }
    }

    public sealed class PartyIdentification_CN
    {
        [Required] public string SchemeID { get; set; } = "";
        [Required] public string Value { get; set; } = "";
    }

    public sealed class PostalAddress_CN
    {
        public string CityName { get; set; } = "";
        public string PostalZone { get; set; } = "";
        public string CountrySubentityCode { get; set; } = "";
        public List<string> AddressLines { get; set; } = new();
        public Country_CN Country { get; set; } = new();
    }

    public sealed class Country_CN
    {
        public string IdentificationCode { get; set; } = "";
        public string? ListID { get; set; }
        public string? ListAgencyID { get; set; }
    }

    public sealed class PartyLegalEntity_CN
    {
        [Required] public string RegistrationName { get; set; } = "";
        public string? CompanyID { get; set; }
    }

    public sealed class Contact_CN
    {
        public string Telephone { get; set; } = "";
        public string ElectronicMail { get; set; } = "";
    }

    public sealed class IndustryClassificationCode_CN
    {
        public string Name { get; set; } = "";
        public string ID { get; set; } = "";
    }

    public sealed class Delivery_CN
    {
        public DeliveryParty_CN DeliveryParty { get; set; } = new();
        public Shipment_CN Shipment { get; set; } = new();
    }

    public sealed class DeliveryParty_CN
    {
        public Party_CN Party { get; set; } = new();
    }

    public sealed class Shipment_CN
    {
        public string ID { get; set; } = "";
        public string HandlingCode { get; set; } = "";
        public string HandlingInstructions { get; set; } = "";
        public FreightAllowanceCharge_CN FreightAllowanceCharge { get; set; } = new();
    }

    public sealed class FreightAllowanceCharge_CN
    {
        public bool ChargeIndicator { get; set; }
        public string AllowanceChargeReason { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public sealed class PaymentMeans_CN
    {
        public string PaymentMeansCode { get; set; } = "";
        public PayeeFinancialAccount_CN PayeeFinancialAccount { get; set; } = new();
    }

    public sealed class PayeeFinancialAccount_CN
    {
        public string ID { get; set; } = "";
    }

    public sealed class PaymentTerms_CN
    {
        public string Note { get; set; } = "";
    }

    public sealed class PrepaidPayment_CN
    {
        public string ID { get; set; } = "";
        public decimal PaidAmount { get; set; }
        public string PaidDate { get; set; } = "";
        public string PaidTime { get; set; } = "";
    }

    public sealed class AllowanceCharge_CN
    {
        public bool ChargeIndicator { get; set; }
        public string AllowanceChargeReason { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public sealed class TaxTotal_CN
    {
        public decimal TaxAmount { get; set; }
        public TaxSubtotal_CN TaxSubtotal { get; set; } = new();
    }

    public sealed class TaxSubtotal_CN
    {
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal? Percent { get; set; }
        public string? TaxExemptionReason { get; set; }
        public TaxCategory_CN TaxCategory { get; set; } = new();
    }

    public sealed class TaxCategory_CN
    {
        public string ID { get; set; } = "";
        public TaxScheme_CN TaxScheme { get; set; } = new();
    }

    public sealed class TaxScheme_CN
    {
        public string SchemeID { get; set; } = "";
        public string SchemeAgencyID { get; set; } = "";
        public string ID { get; set; } = "";
    }

    public sealed class LegalMonetaryTotal_CN
    {
        public decimal LineExtensionAmount { get; set; }
        public decimal TaxExclusiveAmount { get; set; }
        public decimal TaxInclusiveAmount { get; set; }
        public decimal AllowanceTotalAmount { get; set; }
        public decimal ChargeTotalAmount { get; set; }
        public decimal PayableRoundingAmount { get; set; }
        public decimal PayableAmount { get; set; }
    }

    public sealed class InvoiceLine_CN
    {
        public string ID { get; set; } = "";
        public decimal InvoicedQuantity { get; set; }
        public string UnitCode { get; set; } = "C62";
        public decimal LineExtensionAmount { get; set; }
        public List<AllowanceCharge_CN>? AllowanceCharges { get; set; }
        public TaxTotal_CN TaxTotal { get; set; } = new();
        public Item_CN Item { get; set; } = new();
        public Price_CN Price { get; set; } = new();
        public ItemPriceExtension_CN ItemPriceExtension { get; set; } = new();
    }

    public sealed class Item_CN
    {
        public string Description { get; set; } = "";
        public OriginCountry_CN OriginCountry { get; set; } = new();
        public List<CommodityClassification_CN> CommodityClassifications { get; set; } = new();
    }

    public sealed class OriginCountry_CN
    {
        public string IdentificationCode { get; set; } = "";
    }

    public sealed class CommodityClassification_CN
    {
        public string ListID { get; set; } = "";
        public string ItemClassificationCode { get; set; } = "";
    }

    public sealed class Price_CN
    {
        public decimal PriceAmount { get; set; }
        public string CurrencyID { get; set; } = "MYR";
    }

    public sealed class ItemPriceExtension_CN
    {
        public decimal Amount { get; set; }
        public string CurrencyID { get; set; } = "MYR";
    }

    #endregion

    #region XML Builder

    internal static class UblCreditNoteBuilder_CN
    {
        private static readonly XNamespace ubl = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private static readonly XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private static readonly XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        private static XElement Money(string tag, decimal value)
            => new XElement(cbc + tag, new XAttribute("currencyID", "MYR"), value);

        private static XElement E(XName name, object value) => new XElement(name, value);
        private static XElement E(XName name, XAttribute attr, object value) => new XElement(name, attr, value);
        private static XElement E(XName name, XAttribute attr1, XAttribute attr2, object value) => new XElement(name, attr1, attr2, value);
        private static XElement E(XName name, XAttribute attr1, XAttribute attr2, XAttribute attr3, object value) => new XElement(name, attr1, attr2, attr3, value);

        public static XDocument Build(CreditNoteModel_CN model)
        {
            var root = new XElement(ubl + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),

                E(cbc + "ID", model.ID),
                E(cbc + "IssueDate", model.IssueDate.ToString("yyyy-MM-dd")),
                E(cbc + "IssueTime", model.IssueTime),
                new XElement(cbc + "InvoiceTypeCode",
                    new XAttribute("listVersionID", "1.0"),
                    model.InvoiceTypeCode),
                E(cbc + "DocumentCurrencyCode", model.DocumentCurrencyCode),
                E(cbc + "TaxCurrencyCode", model.TaxCurrencyCode),

                from br in model.BillingReferences
                select new XElement(cac + "BillingReference",
                    new XElement(cac + "InvoiceDocumentReference",
                        E(cbc + "ID", br.InvoiceDocumentReference.ID),
                        br.InvoiceDocumentReference.UUID != null ? E(cbc + "UUID", br.InvoiceDocumentReference.UUID) : null),
                    br.AdditionalDocumentReference != null ?
                    new XElement(cac + "AdditionalDocumentReference",
                        E(cbc + "ID", br.AdditionalDocumentReference.ID),
                        br.AdditionalDocumentReference.DocumentType != null ? E(cbc + "DocumentType", br.AdditionalDocumentReference.DocumentType) : null,
                        br.AdditionalDocumentReference.DocumentDescription != null ? E(cbc + "DocumentDescription", br.AdditionalDocumentReference.DocumentDescription) : null) : null),

                from doc in model.AdditionalDocumentReferences
                select new XElement(cac + "AdditionalDocumentReference",
                    E(cbc + "ID", doc.ID),
                    doc.DocumentType != null ? E(cbc + "DocumentType", doc.DocumentType) : null,
                    doc.DocumentDescription != null ? E(cbc + "DocumentDescription", doc.DocumentDescription) : null),

                BuildAccountingSupplierParty(model.AccountingSupplierParty),
                BuildAccountingCustomerParty(model.AccountingCustomerParty),

                model.Delivery != null ? BuildDelivery(model.Delivery) : null,

                model.PaymentMeans != null ? BuildPaymentMeans(model.PaymentMeans) : null,

                model.PaymentTerms != null ? BuildPaymentTerms(model.PaymentTerms) : null,

                from pp in model.PrepaidPayments select BuildPrepaidPayment(pp),

                from ac in model.AllowanceCharges select BuildAllowanceCharge(ac),

                BuildTaxTotal(model.TaxTotal),

                BuildLegalMonetaryTotal(model.LegalMonetaryTotal),

                from line in model.InvoiceLines select BuildInvoiceLine(line)
            );

            return new XDocument(root);
        }

        private static XElement BuildAccountingSupplierParty(AccountingSupplierParty_CN asp)
        {
            return new XElement(cac + "AccountingSupplierParty",
                new XElement(cbc + "AdditionalAccountID",
                    new XAttribute("schemeAgencyName", "CertEX"),
                    asp.AdditionalAccountID),
                BuildParty(asp.Party));
        }

        private static XElement BuildAccountingCustomerParty(AccountingCustomerParty_CN acp)
        {
            return new XElement(cac + "AccountingCustomerParty",
                BuildParty(acp.Party));
        }

        private static XElement BuildParty(Party_CN party)
        {
            return new XElement(cac + "Party",
                party.IndustryClassificationCode != null ?
                    new XElement(cbc + "IndustryClassificationCode",
                        new XAttribute("name", party.IndustryClassificationCode.Name),
                        party.IndustryClassificationCode.ID) : null,
                from id in party.PartyIdentifications
                select new XElement(cac + "PartyIdentification",
                    new XElement(cbc + "ID",
                        new XAttribute("schemeID", id.SchemeID), id.Value)),
                BuildPostalAddress(party.PostalAddress),
                new XElement(cac + "PartyLegalEntity",
                    E(cbc + "RegistrationName", party.PartyLegalEntity.RegistrationName),
                    party.PartyLegalEntity.CompanyID != null ? E(cbc + "CompanyID", party.PartyLegalEntity.CompanyID) : null),
                new XElement(cac + "Contact",
                    E(cbc + "Telephone", party.Contact.Telephone),
                    E(cbc + "ElectronicMail", party.Contact.ElectronicMail))
            );
        }

        private static XElement BuildPostalAddress(PostalAddress_CN addr)
        {
            return new XElement(cac + "PostalAddress",
                E(cbc + "CityName", addr.CityName),
                E(cbc + "PostalZone", addr.PostalZone),
                E(cbc + "CountrySubentityCode", addr.CountrySubentityCode),
                from line in addr.AddressLines select new XElement(cac + "AddressLine", E(cbc + "Line", line)),
                new XElement(cac + "Country",
                    new XElement(cbc + "IdentificationCode",
                        new XAttribute("listID", addr.Country.ListID ?? ""),
                        new XAttribute("listAgencyID", addr.Country.ListAgencyID ?? ""),
                        addr.Country.IdentificationCode))
            );
        }

        private static XElement BuildDelivery(Delivery_CN d)
        {
            return new XElement(cac + "Delivery",
                new XElement(cac + "DeliveryParty",
                    BuildParty(d.DeliveryParty.Party)),
                new XElement(cac + "Shipment",
                    E(cac + "ID", d.Shipment.ID),
                    E(cac + "HandlingCode", d.Shipment.HandlingCode),
                    E(cac + "HandlingInstructions", d.Shipment.HandlingInstructions),
                    new XElement(cac + "FreightAllowanceCharge",
                        E(cbc + "ChargeIndicator", d.Shipment.FreightAllowanceCharge.ChargeIndicator.ToString().ToLower()),
                        E(cbc + "AllowanceChargeReason", d.Shipment.FreightAllowanceCharge.AllowanceChargeReason),
                        Money("Amount", d.Shipment.FreightAllowanceCharge.Amount))
                    )
                );
        }

        private static XElement BuildPaymentMeans(PaymentMeans_CN pm)
        {
            return new XElement(cac + "PaymentMeans",
                E(cbc + "PaymentMeansCode", pm.PaymentMeansCode),
                new XElement(cac + "PayeeFinancialAccount",
                    E(cbc + "ID", pm.PayeeFinancialAccount.ID))
            );
        }

        private static XElement BuildPaymentTerms(PaymentTerms_CN pt)
        {
            return new XElement(cac + "PaymentTerms",
                E(cbc + "Note", pt.Note)
            );
        }

        private static XElement BuildPrepaidPayment(PrepaidPayment_CN pp)
        {
            return new XElement(cac + "PrepaidPayment",
                E(cbc + "ID", pp.ID),
                Money("PaidAmount", pp.PaidAmount),
                E(cbc + "PaidDate", pp.PaidDate),
                E(cbc + "PaidTime", pp.PaidTime));
        }

        private static XElement BuildAllowanceCharge(AllowanceCharge_CN ac)
        {
            return new XElement(cac + "AllowanceCharge",
                E(cbc + "ChargeIndicator", ac.ChargeIndicator.ToString().ToLower()),
                E(cbc + "AllowanceChargeReason", ac.AllowanceChargeReason),
                Money("Amount", ac.Amount));
        }

        private static XElement BuildTaxTotal(TaxTotal_CN tt)
        {
            return new XElement(cac + "TaxTotal",
                Money("TaxAmount", tt.TaxAmount),
                new XElement(cac + "TaxSubtotal",
                    Money("TaxableAmount", tt.TaxSubtotal.TaxableAmount),
                    Money("TaxAmount", tt.TaxSubtotal.TaxAmount),
                    tt.TaxSubtotal.Percent.HasValue ? E(cbc + "Percent", tt.TaxSubtotal.Percent.Value.ToString("F2")) : null,
                    tt.TaxSubtotal.TaxExemptionReason != null ? E(cbc + "TaxExemptionReason", tt.TaxSubtotal.TaxExemptionReason) : null,
                    new XElement(cac + "TaxCategory",
                        E(cbc + "ID", tt.TaxSubtotal.TaxCategory.ID),
                        new XElement(cac + "TaxScheme",
                            E(cbc + "ID",
                                new XAttribute("schemeID", tt.TaxSubtotal.TaxCategory.TaxScheme.SchemeID),
                                new XAttribute("schemeAgencyID", tt.TaxSubtotal.TaxCategory.TaxScheme.SchemeAgencyID),
                                tt.TaxSubtotal.TaxCategory.TaxScheme.ID)))
                ));
        }

        private static XElement BuildLegalMonetaryTotal(LegalMonetaryTotal_CN lmt)
        {
            return new XElement(cac + "LegalMonetaryTotal",
                Money("LineExtensionAmount", lmt.LineExtensionAmount),
                Money("TaxExclusiveAmount", lmt.TaxExclusiveAmount),
                Money("TaxInclusiveAmount", lmt.TaxInclusiveAmount),
                Money("AllowanceTotalAmount", lmt.AllowanceTotalAmount),
                Money("ChargeTotalAmount", lmt.ChargeTotalAmount),
                Money("PayableRoundingAmount", lmt.PayableRoundingAmount),
                Money("PayableAmount", lmt.PayableAmount)
            );
        }

        private static XElement BuildInvoiceLine(InvoiceLine_CN line)
        {
            return new XElement(cac + "InvoiceLine",
                E(cbc + "ID", line.ID),
                new XElement(cbc + "InvoicedQuantity",
                    new XAttribute("unitCode", line.UnitCode),
                    line.InvoicedQuantity),
                Money("LineExtensionAmount", line.LineExtensionAmount),

                line.AllowanceCharges != null ?
                from ac in line.AllowanceCharges
                select BuildAllowanceCharge(ac)
                : null,

                BuildTaxTotal(line.TaxTotal),

                new XElement(cac + "Item",
                    E(cbc + "Description", line.Item.Description),
                    new XElement(cac + "OriginCountry",
                        E(cbc + "IdentificationCode", line.Item.OriginCountry.IdentificationCode)),
                    from cc in line.Item.CommodityClassifications
                    select new XElement(cac + "CommodityClassification",
                        new XElement(cbc + "ItemClassificationCode",
                            new XAttribute("listID", cc.ListID),
                            cc.ItemClassificationCode))
                ),

                new XElement(cac + "Price",
                    new XElement(cbc + "PriceAmount",
                        new XAttribute("currencyID", line.Price.CurrencyID),
                        line.Price.PriceAmount)),

                new XElement(cac + "ItemPriceExtension",
                    new XElement(cbc + "Amount",
                        new XAttribute("currencyID", line.ItemPriceExtension.CurrencyID),
                        line.ItemPriceExtension.Amount))
            );
        }
    }

    #endregion
}

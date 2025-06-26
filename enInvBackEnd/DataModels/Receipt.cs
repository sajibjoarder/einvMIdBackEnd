using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class Receipt
{
    public Guid ReceiptId { get; set; }

    public string ReceiptNumber { get; set; } = null!;

    public DateOnly DateOfIssue { get; set; }

    public string SellerName { get; set; } = null!;

    public string? SellerLogoUrl { get; set; }

    public string? SellerAddress { get; set; }

    public string? SellerContact { get; set; }

    public string? BuyerName { get; set; }

    public string? BuyerAddress { get; set; }

    public decimal Subtotal { get; set; }

    public decimal? Discount { get; set; }

    public decimal? Tax { get; set; }

    public decimal TotalAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentReferenceId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? CompanyId { get; set; }

    public bool? Submitted { get; set; }

    public Guid? Docid { get; set; }

    public string? DocType { get; set; }

    public virtual ICollection<ReceiptItem> ReceiptItems { get; set; } = new List<ReceiptItem>();
}

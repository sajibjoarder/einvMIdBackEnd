using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;
public partial class ReceiptItemDetail
{
    public Guid? ItemId { get; set; }

    public Guid? ReceiptId { get; set; }

    public string? ItemDescription { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public Guid? ProductId { get; set; }

    public string? ProductName { get; set; }

    public string? ItemClassificationCode { get; set; }

    public string? Uom { get; set; }

    public decimal? ProductPrice { get; set; }

    public double? ProductStock { get; set; }

    public Guid? CompanyId { get; set; }
}

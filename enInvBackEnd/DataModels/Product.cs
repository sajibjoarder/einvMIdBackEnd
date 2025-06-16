using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class Product
{
    public string? ProductName { get; set; }

    public string? ItemClassificationCode { get; set; }

    public string? Uom { get; set; }

    public decimal? Price { get; set; }

    public double? Quantity { get; set; }

    public Guid ProductId { get; set; }

    public Guid? CompanyId { get; set; }
}

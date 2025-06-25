using System;
using System.Collections.Generic;

namespace enInvBackEnd.DataModels;

public partial class ReceiptItem
{
    public Guid ItemId { get; set; }

    public Guid? ReceiptId { get; set; }

    public string ItemDescription { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Receipt? Receipt { get; set; }
}

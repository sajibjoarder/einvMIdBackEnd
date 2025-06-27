using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace enInvBackEnd.DataModels;

public partial class ReceiptItem
{
    public Guid ItemId { get; set; }

    public Guid? ReceiptId { get; set; }

    public string ItemDescription { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public Guid? ProductId { get; set; }

    [JsonIgnore]
    public virtual Receipt? Receipt { get; set; }
}

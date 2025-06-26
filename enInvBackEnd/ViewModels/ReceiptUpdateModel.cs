namespace enInvBackEnd.ViewModels
{
    public class ReceiptUpdateModel
    {
        public Guid ReceiptId { get; set; }
        public bool? Submitted { get; set; }
        public Guid? Docid { get; set; }
        public string? DocType { get; set; }
    }

}

namespace Domain.DLL.Models
{
    public class TSendOrderResult
    {
        public NResult Result { get; set; }
        public long LocalId { get; set; }
        public bool IsSuccessful { get; set; }
    }
}

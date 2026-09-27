namespace Domain.TickTest.Models
{
    public struct Agent
    {
        public int Id;
        public string Name;

        public override string ToString() => $"{Name} ({Id})";
    }
}

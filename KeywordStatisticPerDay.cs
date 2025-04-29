namespace TPMRTweb.Models
{
    public class KeywordStatisticPerDay
    {
        public DateTime Date { get; set; }
        public Dictionary<string, int> KeywordCounts { get; set; }
    }
}

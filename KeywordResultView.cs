namespace TPMRTweb.Models
{
    public class KeywordResultView
    {
        public List<KeywordStatisticPerDay> StatisticsPerDay { get; set; } = new();
        public Dictionary<string, string> KeywordDisplayNames { get; set; } = new();
    }
}
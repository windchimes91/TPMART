namespace TPMRTweb.Models
{
    public class TrainTransferRecord
    {
        public int Id { get; set; }
        public int TrainId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public Train Train { get; set; }
    }
}

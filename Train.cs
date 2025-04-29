namespace TPMRTweb.Models
{
    public class Train
    {
        public int TrainId { get; set; }
        public string TrainNo { get; set; }

        public ICollection<TrainTransferRecord> TransferRecords { get; set; }
    }
}

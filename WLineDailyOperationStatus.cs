namespace TPMRTweb.Models
{
    public class WLineDailyOperationStatus
    {
        public string FormattedDate { get; set; }
        public string Weather { get; set; }

        //一、營運重要事件摘錄
        public List<EventGroup> EventGroups { get; set; } = new List<EventGroup>();
        public string MainController { get; set; }


        //二、行車延誤事件統計
        public int U5ControllableCountToday { get; set; }
        public int U5UncontrollableCountToday { get; set; }
        public int D5ControllableCountToday { get; set; }
        public int D5UncontrollableCountToday { get; set; }



        //三、急待解決事項
        public string Pending { get; set; }


        //四、電聯車使用概況
        public string MaxMorningPeakPeriod { get; set; }
        public string MaxAfternoonPeakPeriod { get; set; }
        public string MaxMorningOffPeakPeriod { get; set; }
        public string MaxAfternoonOffPeakPeriod { get; set; }
        public string RegularMorningOvertimeWork { get;set; }
        public string RegularAfternoonOvertimeWork { get; set; }
        public string TemporaryMorningOvertimeWork { get; set; }
        public string TemporaryAfternoonOvertimeWork { get; set; }
        public string AvailableMorningPeak { get; set; }
        public string AvailableAfternoonPeak { get; set; }
        public string AvailableOffPeak { get; set; }
        public string AvailableMorningPeakBT { get; set; }
        public string AvailableAfternoonPeakBT { get; set; }
        public string AvailableOffPeakBT { get; set; }


        //五、運行績效
        public int U5ControllableCountYear { get; set; }
        public int U5UncontrollableCountYear { get; set; }
        public string U5Uncontrollable { get; set; }
        public string U5Controllable { get; set; }
        public string TrainKmOnTheDay { get; set; }
        public string TrainKmAccumulated { get; set; }
        public string MorningPeakDirection1 { get; set; }
        public string AfternoonPeakDirection2 { get; set; }
        public string PeakDistance { get; set; }
        public string OffPeakDistance { get; set; }
        public string OvertimeOperationLine { get; set; }

        //（四）發車次數
        public string OvertimeDate { get; set; }
        public string OvertimeWorkType { get; set; }
        public string OvertimeTrainWork { get; set; }
        public string OvertimeTrainStation { get; set; }
        public string OvertimeTrainKm { get; set; }
        public string OvertimeTrainDeparture { get; set; }






        //六、未載客列車里程數（公里）
        public string MorningTrainKm { get; set; }
        public string PeakOffPeakTrainKm { get; set; }
        public string NightTrainKm { get; set; }
        public string TableOrderOvertimeTrainKm { get; set; }
        public string TransferTrainKm { get; set; }
        public string TableOrderMaintenanceTrainKm { get; set; }
        public string TemporaryOvertimeTrainKm { get; set; }
        public string TemporaryMaintenanceTrainKm { get; set; }
        public string ExceptionEventKm { get; set; }
        public string OtherUnloadedKm { get; set; }
        public string TodayKmTotal { get; set; }
        public string TotalUnloadedKm { get; set; }


        //七、本日文湖線運量
        public string TodayTrainTotal { get; set; }


    }

    public class EventGroup
    {
        public string Title { get; set; }
        public List<TimeEntryPair> TimeEntries { get; set; } = new List<TimeEntryPair>();
    }

    public class TimeEntryPair
    {
        public string Time { get; set; }
        public string Content { get; set; }
    }
}


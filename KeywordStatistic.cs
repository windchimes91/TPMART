
using System.ComponentModel.DataAnnotations;

namespace TPMRTweb.Models
{
    public class KeywordStatistic
    {
        [Key]
        public int Id { get; set; }

        public string FileName { get; set; }     // 上傳的檔名
        public DateTime Date { get; set; }         // 報表日期
        public string Keyword { get; set; }      // 原始關鍵字
        public int Count { get; set; }           // 出現次數
        public DateTime UploadedAt { get; set; } // 上傳時間
    }
}

using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using TPMRTweb.Models;
using Microsoft.EntityFrameworkCore;


public class TrainTransferRecordController : Controller
{
    private readonly TPMRTContext _context;

    public TrainTransferRecordController(TPMRTContext context)
    {
        _context = context;
    }
    public IActionResult Index()
    {
        ViewBag.Message = "請先選擇欲查詢的日期區間";
        return View(new List<TrainTransferRecord>());
    }

    public IActionResult SearchByDate(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
        {
            ViewBag.Message = "請選擇開始和結束日期";
            return View("Index", new List<TrainTransferRecord>());
        }

        if (startDate > endDate)
        {
            ViewBag.Message = "開始日期不能晚於結束日期";
            return View("Index", new List<TrainTransferRecord>());
        }

        // 設定時間範圍的起始和結束
        var startDateTime = startDate.Value.Date;
        var endDateTime = endDate.Value.Date.AddDays(1).AddSeconds(-1); // 設為當天最後一秒

        // 查詢資料庫，根據 StartTime 日期範圍過濾
        var records = _context.TrainTransferRecords
                             .Include(r => r.Train)
                             .Where(r => r.StartTime >= startDateTime && r.StartTime <= endDateTime)
                             .ToList();

        if (!records.Any())
        {
            ViewBag.Message = "查詢區間內無資料";
        }

        ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

        return View("Index", records);
    }

   
}

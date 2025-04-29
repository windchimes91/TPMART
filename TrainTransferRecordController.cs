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
        var records = _context.TrainTransferRecords
                             .Include(r => r.Train)
                             .ToList();
        return View(records);
    }


    public IActionResult Upload(IFormFile file)
    {
        var records = new List<TrainTransferRecord>();

        if (file != null && file.Length > 0)
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header
            foreach (var row in rows)
            {
                var trainNo = row.Cell(1).GetString();

                // Assuming columns are TrainNo, StartTime1, EndTime1, StartTime2, EndTime2, etc.
                // Adjust the loop to handle pairs of start/end times
                for (int i = 0; i < (worksheet.LastColumnUsed().ColumnNumber() - 1) / 2; i++)
                {
                    var startCell = row.Cell(2 + i * 2);
                    var endCell = row.Cell(3 + i * 2);

                    var startValue = startCell.GetValue<string>();
                    var endValue = endCell.GetValue<string>();

                    if (TimeSpan.TryParse(startValue, out TimeSpan startTime) &&
                        TimeSpan.TryParse(endValue, out TimeSpan endTime))
                    {
                        // Find or create the Train entity
                        var train = _context.Trains.FirstOrDefault(t => t.TrainNo == trainNo);
                        if (train == null)
                        {
                            train = new Train { TrainNo = trainNo };
                            _context.Trains.Add(train);
                            _context.SaveChanges(); // Save to get TrainId
                        }

                        records.Add(new TrainTransferRecord
                        {
                            TrainId = train.TrainId, // Set the foreign key
                            Train = train, // Optional: set navigation property
                            StartTime = startTime,
                            EndTime = endTime
                        });
                    }
                }
            }

            // Save records to the database
            _context.TrainTransferRecords.AddRange(records);
            _context.SaveChanges();
        }

        return View("Index", records);
    }
}
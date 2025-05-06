using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using TPMRTweb.Models;
using ClosedXML.Excel;
using System.Text.Json;
using System.Globalization;

namespace TPMRTweb.Controllers
{
    public class KeyWordController : Controller
    {
        private readonly TPMRTContext _context;

        public KeyWordController(TPMRTContext context)
        {
            _context = context;
        }

        private static readonly List<string> Keywords = new()
        {
            "位置錯誤", "RATC Communication Failure", "Communication Failure", "煞車系統", "列車門",
            "月台門", "推進系統", "喪失維生零速", "低胎壓", "過站不停", "列車E.B.", "No Init", "No Communication"
        };

        private static readonly Dictionary<string, string> KeywordDisplayNames = new()
        {
            { "位置錯誤", "位置錯誤" },
            { "RATC Communication Failure", "RATC Comm Failure" },
            { "Communication Failure", "Train Comm Failure" },
            { "煞車系統", "煞車系統" },
            { "列車門", "列車門" },
            { "月台門", "月台門" },
            { "推進系統", "推進系統" },
            { "喪失維生零速", "喪失維生零速" },
            { "低胎壓", "低胎壓" },
            { "過站不停", "過站不停" },
            { "列車E.B.", "列車E.B." },
            { "No Init", "No Init" },
            { "No Communication", "No Comm" }
        };

        public IActionResult Index()
        {
            // 初始頁面不顯示任何資料
            var viewModel = new KeywordResultView
            {
                StatisticsPerDay = new List<KeywordStatisticPerDay>(),
                KeywordDisplayNames = KeywordDisplayNames
            };

            ViewBag.Message = "請先選擇欲查詢的日期區間";
            return View(viewModel);
        }




        private byte[] ConvertDocToDocxWithAspose(Stream docStream)
        {
            var doc = new Aspose.Words.Document(docStream);
            using var outStream = new MemoryStream();
            doc.Save(outStream, Aspose.Words.SaveFormat.Docx);
            return outStream.ToArray();
        }

        public IActionResult Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Message = "請選擇一個 Word 檔案，僅支援 doc 或 docx 檔";
                return View("Index");
            }

            string extension = Path.GetExtension(file.FileName).ToLower();

            byte[] docxBytes;
            if (extension == ".doc")
            {
                docxBytes = ConvertDocToDocxWithAspose(file.OpenReadStream());
            }
            else if (extension == ".docx")
            {
                using var ms = new MemoryStream();
                file.CopyTo(ms);
                docxBytes = ms.ToArray();
            }
            else
            {
                ViewBag.Message = "不支援的檔案格式，請上傳 doc 或 docx 檔案";
                return View("Index");
            }

            Dictionary<string, int> keywordCounts = Keywords.ToDictionary(k => k, v => 0);
            string extractedDate = "";

            using (var stream = new MemoryStream(docxBytes))
            {
                using (var wp = WordprocessingDocument.Open(stream, false))
                {
                    var doc = wp.MainDocumentPart?.Document;
                    if (doc?.Body == null)
                    {
                        ViewBag.Message = "文件內容無效或為空";
                        return View("Index");
                    }

                    var tables = doc.Body.Elements<Table>().ToList();
                    if (tables.Count == 0)
                    {
                        ViewBag.Message = "未找到表格";
                        return View("Index");
                    }

                    foreach (var table in tables)
                    {
                        var dataRow = table.Elements<TableRow>().Skip(1).FirstOrDefault();
                        if (dataRow != null)
                        {
                            var cells = dataRow.Elements<TableCell>().ToArray();
                            if (cells.Length > 1)
                            {
                                var rawText = cells[1].InnerText ?? "";
                                var yearSplit = rawText.Split("年");
                                if (yearSplit.Length > 1)
                                {
                                    var datePart = yearSplit[1].Split(" ").FirstOrDefault();
                                    if (!string.IsNullOrWhiteSpace(datePart))
                                    {
                                        extractedDate = $"{yearSplit[0]}年{datePart}";
                                    }
                                }
                            }
                        }

                        foreach (var tr in table.Elements<TableRow>().Skip(6))
                        {
                            var tds = tr.Elements<TableCell>().ToArray();
                            if (tds.Length > 4)
                            {
                                string narrate = tds[4].InnerText.Trim();
                                foreach (var keyword in Keywords)
                                {
                                    if (narrate.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        keywordCounts[keyword]++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            DateTime? documentDate = null;

            if (!string.IsNullOrWhiteSpace(extractedDate))
            {
                var taiwanYearSplit = extractedDate.Split("年");
                if (taiwanYearSplit.Length > 1 && int.TryParse(taiwanYearSplit[0], out int taiwanYear))
                {
                    int gregorianYear = taiwanYear + 1911;
                    var monthDayPart = taiwanYearSplit[1].Replace("月", "-").Replace("日", "");

                    var parts = monthDayPart.Split('-');
                    if (parts.Length == 2)
                    {
                        var month = int.Parse(parts[0]).ToString("D2");
                        var day = int.Parse(parts[1]).ToString("D2");
                        var formattedDate = $"{gregorianYear}-{month}-{day}";

                        // 這裡正式轉成 DateTime
                        if (DateTime.TryParseExact(formattedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                        {
                            documentDate = parsedDate;
                        }
                    }
                }
            }
            else
            {
                ViewBag.Message = "未能提取到有效的日期。";
                return View("Index");
            }


            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var uploadTime = DateTime.Now;

            foreach (var kvp in keywordCounts)
            {
                if (kvp.Value > 0)
                {
                    var record = new KeywordStatistic
                    {
                        FileName = fileName,
                        Date = documentDate.Value,
                        Keyword = kvp.Key,
                        Count = kvp.Value,
                        UploadedAt = uploadTime
                    };
                    _context.KeywordStatistics.Add(record);
                }
            }
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult QueryByDateRange(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                TempData["Message"] = "開始日期不能晚於結束日期。";
                return RedirectToAction("Index");
            }

            var keywordStats = _context.KeywordStatistics
                .Where(k => k.Date.Date >= startDate.Date && k.Date.Date <= endDate.Date)  // 只比較日期部分
                .AsEnumerable()  // 確保進一步處理在內存中進行
                .GroupBy(k => k.Date.Date)  // 按日期分組，忽略時間部分
                .OrderBy(g => g.Key)
                .Select(g => new KeywordStatisticPerDay
                {
                    Date = g.Key,
                    KeywordCounts = g
                        .GroupBy(x => x.Keyword)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Sum(y => y.Count)
                        )
                })
                .ToList();

            if (!keywordStats.Any())
            {
                TempData["Message"] = "這個時間區間內無資料。";
                return RedirectToAction("Index");
            }

            var viewModel = new KeywordResultView
            {
                StatisticsPerDay = keywordStats,
                KeywordDisplayNames = KeywordDisplayNames
            };

            TempData["KeywordResult"] = JsonSerializer.Serialize(viewModel);
            return View("Index", viewModel);
        }



        public IActionResult ExportToExcel()
        {
            KeywordResultView viewModel;

            // 檢查是否有查詢結果
            if (TempData.ContainsKey("KeywordResult"))
            {
                var json = TempData["KeywordResult"] as string;
                TempData.Keep("KeywordResult"); // 保留 TempData 以便可以在頁面重新載入後使用

                if (!string.IsNullOrEmpty(json))
                {
                    viewModel = JsonSerializer.Deserialize<KeywordResultView>(json);
                    if (viewModel == null || viewModel.StatisticsPerDay == null || !viewModel.StatisticsPerDay.Any())
                    {
                        // 如果反序列化失敗或數據為空，則使用所有數據
                        viewModel = GetAllKeywordStatistics();
                    }
                }
                else
                {
                    // 如果 JSON 為空，則使用所有數據
                    viewModel = GetAllKeywordStatistics();
                }
            }
            else
            {
                // 如果沒有查詢結果，則匯出所有數據
                viewModel = GetAllKeywordStatistics();
            }

            if (viewModel == null || viewModel.StatisticsPerDay == null || !viewModel.StatisticsPerDay.Any())
            {
                TempData["Message"] = "無資料可匯出。";
                return RedirectToAction("Index");
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("資料");

            string[] headers = {
                "故障類別", "Location Failure", "RATC Comm Failure", "Train Comm Failure",
                "Brake System", "Train Door System", "Platform Door System", "Train P.P. System",
                "Loss of Vital Zero Speed", "Low Tire Pressure", "過站不停", "列車E.B.", "TRS No Init", "TRS No Comm"
            };


            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];

                var style = cell.Style;
                style.Fill.BackgroundColor = XLColor.FromHtml("#FFCC99");  // 背景色
                style.Font.FontName = "新細明體";                         // 字型
                style.Font.FontSize = 12;                                  // 字體大小
                style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // 水平置中
                style.Alignment.Vertical = XLAlignmentVerticalValues.Center;     // 垂直置中
                style.Alignment.WrapText = true;                          // 自動換行
                style.Border.OutsideBorder = XLBorderStyleValues.Thin;    // 外框
                style.Border.InsideBorder = XLBorderStyleValues.Thin;     // 內框（如果合併的話就無效）
            }

            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cell(2, col);
                int[] Gtotal = { 1872, 1103, 3640, 6027, 1544, 485, 1921, 1419, 257, 325, 784, 1049, 76 };
                if (col == 1)
                    cell.Value = "2009/10/25至今日累計";
                else
                    cell.Value = Gtotal[col - 2];

                var style = cell.Style;
                style.Fill.BackgroundColor = XLColor.FromHtml("#FFCC99");
                style.Font.FontName = "新細明體";
                style.Font.FontSize = 12;
                style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cell(4, col);
                int[] Gtotal = { 1720, 1039, 3076, 5675, 1398, 485, 1782, 1255, 246, 299, 769, 910, 72 };
                if (col == 1)
                    cell.Value = "2009年10月25日至2022年7月31日累計";
                else
                    cell.Value = Gtotal[col - 2];

                var style = cell.Style;
                style.Font.FontName = "新細明體";
                style.Font.FontSize = 12;
                style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // 設定標題列
            worksheet.Cell(3, 1).Value = "日期";
            int column = 2;
            // 使用 KeywordDisplayNames 顯示名稱作為列標題
            foreach (var keyword in viewModel.KeywordDisplayNames)
            {
                var cell = worksheet.Cell(3, column);
                cell.Value = keyword.Value;  // 使用 Value (顯示名稱)，而不是 Key
                column++;

                var style = cell.Style;
                style.Font.FontName = "新細明體";
                style.Font.FontSize = 12;
                style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // 填充各日期的數據
            int row = 5;
            foreach (var dayStat in viewModel.StatisticsPerDay)
            {
                // 設定每行樣式
                for (int col = 1; col <= viewModel.KeywordDisplayNames.Count + 1; col++)
                {
                    var cell = worksheet.Cell(row, col);
                    var style = cell.Style;
                    style.Font.FontName = "新細明體";
                    style.Font.FontSize = 12;
                    style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    style.Border.InsideBorder = XLBorderStyleValues.Thin;
                }

                worksheet.Cell(row, 1).Value = dayStat.Date.ToString("yyyy-MM-dd");

                // 填充每個關鍵字的計數
                column = 2;
                foreach (var keyword in viewModel.KeywordDisplayNames.Keys)
                {
                    worksheet.Cell(row, column).Value = dayStat.KeywordCounts.ContainsKey(keyword)
                        ? dayStat.KeywordCounts[keyword]
                        : 0;
                    column++;
                }
                row++;

            }


            // 添加總計行
            worksheet.Cell(row, 1).Value = "總計";

            // 計算每個關鍵字的總數
            column = 2;
            foreach (var keyword in viewModel.KeywordDisplayNames.Keys)
            {
                int keywordTotal = 0;
                foreach (var dayStat in viewModel.StatisticsPerDay)
                {
                    if (dayStat.KeywordCounts.ContainsKey(keyword))
                    {
                        keywordTotal += dayStat.KeywordCounts[keyword];
                    }
                }
                worksheet.Cell(row, column).Value = keywordTotal;
                column++;
            }

            // 設定總計行樣式
            for (int col = 1; col <= viewModel.KeywordDisplayNames.Count + 1; col++)
            {
                var cell = worksheet.Cell(row, col);
                var style = cell.Style;

                style.Font.FontName = "新細明體";
                style.Font.FontSize = 12;
                style.Fill.BackgroundColor = XLColor.LightGray; // 背景色
                style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }


            // 自動調整欄位寬度以符合內容
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"故障統計表_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        // 新增一個輔助方法來獲取所有關鍵字統計資料
        private KeywordResultView GetAllKeywordStatistics()
        {
            var allKeywordStats = _context.KeywordStatistics.ToList();

            var keywordStats = allKeywordStats
                .GroupBy(k => k.Date.Date)
                .OrderBy(g => g.Key)
                .Select(g => new KeywordStatisticPerDay
                {
                    Date = g.Key,
                    KeywordCounts = g
                        .GroupBy(x => x.Keyword)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Sum(y => y.Count)
                        )
                })
                .ToList();

            // 直接使用靜態定義的 KeywordDisplayNames
            return new KeywordResultView
            {
                StatisticsPerDay = keywordStats,
                KeywordDisplayNames = KeywordDisplayNames  // 使用類別級別定義的 KeywordDisplayNames
            };
        }


    }


}


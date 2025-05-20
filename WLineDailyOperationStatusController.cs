using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using System.Text;
using System;
using TPMRTweb.Models;
using System.Globalization;
using DocumentFormat.OpenXml;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using DocumentFormat.OpenXml.EMMA;


namespace TPMRTweb.Controllers
{
    public class WLineDailyOperationStatusController : Controller
    {
        private readonly TPMRTContext _context;

        public WLineDailyOperationStatusController(TPMRTContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var now = DateTime.Now;
            var yesterday = now.AddDays(-1);
            var taiwanCulture = new CultureInfo("zh-TW");
            var rocYear = yesterday.Year - 1911;
            var dayOfWeek = yesterday.ToString("dddd", taiwanCulture);
            var dateview = $"民國 {rocYear} 年 {yesterday.Month} 月 {yesterday.Day} 日 {dayOfWeek}";

            var model = new WLineDailyOperationStatus
            {
                FormattedDate = dateview,
            };

            return View(model); // 確保 model 不為 null
        }


        public IActionResult Preview(WLineDailyOperationStatus model)
        {
            var now = DateTime.Now;
            var yesterday = now.AddDays(-1);
            var taiwanCulture = new CultureInfo("zh-TW");
            var rocYear = yesterday.Year - 1911;
            var dayOfWeek = yesterday.ToString("dddd", taiwanCulture);

            var dateview = $"民國 {rocYear} 年 {yesterday.Month} 月 {yesterday.Day} 日 {dayOfWeek}";

            //二、行車延誤事件統計
            var startOfYesterday = yesterday.Date;
            var endOfYesterday = yesterday.Date.AddDays(1).AddTicks(-1);

            int U5Controllablecount = _context.RpEventDetails
                .Where(e => e.Date.HasValue && e.Date.Value >= startOfYesterday && e.Date.Value <= endOfYesterday && e.U5controllable == true)
                .Count();
            int U5uncontrollablecount = _context.RpEventDetails
                .Where(e => e.Date.HasValue && e.Date.Value >= startOfYesterday && e.Date.Value <= endOfYesterday && e.U5uncontrollable == true)
                .Count();
            int D5controllablecount = _context.RpEventDetails
                .Where(e => e.Date.HasValue && e.Date.Value >= startOfYesterday && e.Date.Value <= endOfYesterday && e.D5controllable == true)
                .Count();
            int D5uncontrollablecount = _context.RpEventDetails
                .Where(e => e.Date.HasValue && e.Date.Value >= startOfYesterday && e.Date.Value <= endOfYesterday && e.D5uncontrollable == true)
                .Count();

            //五、運行績效
            int targetYear = yesterday.Year;

            // 調試資料
            var allRecordsForYear = _context.RpEventDetails
                .Where(e => e.Date.HasValue && e.Date.Value.Year == targetYear)
                .ToList();


            if (allRecordsForYear.Any())
            {
                var sampleRecords = allRecordsForYear.Take(3);
                foreach (var record in sampleRecords)
                {
                    Console.WriteLine($"Debug - Sample record: Date={record.Date}, U5controllable={record.U5controllable}");
                }
            }

            // 嘗試使用更寬鬆的條件
            int U5ControllableYearcount = _context.RpEventDetails
                .Where(e => e.Date.HasValue && e.Date.Value.Year == targetYear)
                .Count(e => e.U5controllable == true);

            int U5UncontrollableYearcount = _context.RpEventDetails
                .Where(e => e.Date.HasValue && e.Date.Value.Year == targetYear)
                .Count(e => e.U5uncontrollable == true);


            // 嘗試使用原生SQL
            int U5sqlCount;
            int U5UsqlCount;
            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(*) FROM RP_EventDetails WHERE YEAR(Date) = {targetYear} AND U5controllable = 1";
                    var U5result = command.ExecuteScalar();
                    U5sqlCount = U5result != null ? Convert.ToInt32(U5result) : 0;

                    command.CommandText = $"SELECT COUNT(*) FROM RP_EventDetails WHERE YEAR(Date) = {targetYear} AND U5Uncontrollable = 1";
                    var U5Uresult = command.ExecuteScalar();
                    U5UsqlCount = U5Uresult != null ? Convert.ToInt32(U5Uresult) : 0;

                }
            }

            


            var savedModel = model;

            var totalkm = savedModel.MorningTrainKm +
                savedModel.PeakOffPeakTrainKm +
                savedModel.NightTrainKm +
                savedModel.TableOrderOvertimeTrainKm +
                savedModel.TransferTrainKm +
                savedModel.TableOrderMaintenanceTrainKm +
                savedModel.TemporaryOvertimeTrainKm +
                savedModel.TemporaryMaintenanceTrainKm +
                savedModel.ExceptionEventKm +
                savedModel.OtherUnloadedKm;



            var viewModel = new WLineDailyOperationStatus
            {
                // 來自資料庫查詢的數據
                U5ControllableCountToday = U5Controllablecount,
                U5UncontrollableCountToday = U5uncontrollablecount,
                D5ControllableCountToday = D5controllablecount,
                D5UncontrollableCountToday = D5uncontrollablecount,
                U5ControllableCountYear = U5sqlCount,
                U5UncontrollableCountYear = U5UsqlCount,
                FormattedDate = dateview,

                // 保留從 Index 表單傳過來的數據
                Weather = savedModel.Weather,
                MainController = savedModel.MainController,
                EventGroups = savedModel.EventGroups,
                Pending = savedModel.Pending,

                // 電聯車使用概況
                MaxMorningPeakPeriod = savedModel.MaxMorningPeakPeriod,
                MaxAfternoonPeakPeriod = savedModel.MaxAfternoonPeakPeriod,
                MaxMorningOffPeakPeriod = savedModel.MaxMorningOffPeakPeriod,
                MaxAfternoonOffPeakPeriod = savedModel.MaxAfternoonOffPeakPeriod,
                RegularMorningOvertimeWork = savedModel.RegularMorningOvertimeWork,
                RegularAfternoonOvertimeWork = savedModel.RegularAfternoonOvertimeWork,
                TemporaryMorningOvertimeWork = savedModel.TemporaryMorningOvertimeWork,
                TemporaryAfternoonOvertimeWork = savedModel.TemporaryAfternoonOvertimeWork,
                AvailableMorningPeak = savedModel.AvailableMorningPeak,
                AvailableAfternoonPeak = savedModel.AvailableAfternoonPeak,
                AvailableOffPeak = savedModel.AvailableOffPeak,
                AvailableMorningPeakBT = savedModel.AvailableMorningPeakBT,
                AvailableAfternoonPeakBT = savedModel.AvailableAfternoonPeakBT,
                AvailableOffPeakBT = savedModel.AvailableOffPeakBT,

                // 運行績效
                U5Uncontrollable = savedModel.U5Uncontrollable,
                U5Controllable = savedModel.U5Controllable,
                TrainKmOnTheDay = savedModel.TrainKmOnTheDay,
                TrainKmAccumulated = savedModel.TrainKmAccumulated,
                MorningPeakDirection1 = savedModel.MorningPeakDirection1,
                AfternoonPeakDirection2 = savedModel.AfternoonPeakDirection2,
                PeakDistance = savedModel.PeakDistance,
                OffPeakDistance = savedModel.OffPeakDistance,

                // 發車次數
                OvertimeDate = savedModel.OvertimeDate,
                OvertimeWorkType = savedModel.OvertimeWorkType,
                OvertimeOperationLine = savedModel.OvertimeOperationLine,
                OvertimeTrainWork = savedModel.OvertimeTrainWork,
                OvertimeTrainStation = savedModel.OvertimeTrainStation,
                OvertimeTrainKm = savedModel.OvertimeTrainKm,
                OvertimeTrainDeparture = savedModel.OvertimeTrainDeparture,

                // 未載客列車里程數
                MorningTrainKm = savedModel.MorningTrainKm,
                PeakOffPeakTrainKm = savedModel.PeakOffPeakTrainKm,
                NightTrainKm = savedModel.NightTrainKm,
                TableOrderOvertimeTrainKm = savedModel.TableOrderOvertimeTrainKm,
                TransferTrainKm = savedModel.TransferTrainKm,
                TableOrderMaintenanceTrainKm = savedModel.TableOrderMaintenanceTrainKm,
                TemporaryOvertimeTrainKm = savedModel.TemporaryOvertimeTrainKm,
                TemporaryMaintenanceTrainKm = savedModel.TemporaryMaintenanceTrainKm,
                ExceptionEventKm = savedModel.ExceptionEventKm,
                OtherUnloadedKm = savedModel.OtherUnloadedKm,
                TodayKmTotal = savedModel.TodayKmTotal,
                TotalKm = totalkm,

                // 本日文湖線運量
                TodayTrainTotal = savedModel.TodayTrainTotal
            };

            //_context.WLineDailyOperationStatus.Add(viewModel);
            //_context.SaveChanges();

            return View(viewModel);
        }


        public IActionResult ExportToWord(WLineDailyOperationStatus model)
        {
            using (var ms = new MemoryStream())
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
                {
                    MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = new Body();

                    body.AppendChild(new Paragraph(new Run(new Text("文湖線每日行車運轉狀況"))));

                    // 日期（格式可自訂）
                    var now = DateTime.Now;
                    var rocYear = now.Year - 1911;
                    var dayOfWeek = now.ToString("dddd", new System.Globalization.CultureInfo("zh-TW"));
                    var rocDate = $"民國 {rocYear} 年 {now.Month} 月 {now.Day} 日（{dayOfWeek}）";

                    // 加入日期和主任控制員
                    body.AppendChild(new Paragraph(new Run(new Text("日期：" + rocDate))));

                    body.AppendChild(new Paragraph(new Run(new Text("一、營運重要事件摘錄"))));

                    if (!string.IsNullOrEmpty(model.MainController))
                    {
                        body.AppendChild(new Paragraph(new Run(new Text("主任控制員：" + model.MainController))));
                    }

                    body.AppendChild(new Paragraph()); // 空行

                    // 處理每個事件群組
                    if (model.EventGroups != null && model.EventGroups.Any())
                    {
                        for (int i = 0; i < model.EventGroups.Count; i++)
                        {
                            var group = model.EventGroups[i];

                            // 加入群組標題
                            body.AppendChild(new Paragraph(new Run(new Text($"{i + 1}. {group.Title}"))));

                            // 處理每個時間和內容對
                            if (group.TimeEntries != null && group.TimeEntries.Any())
                            {
                                foreach (var entry in group.TimeEntries)
                                {
                                    body.AppendChild(new Paragraph(new Run(new Text($"時間：{entry.Time}"))));
                                    body.AppendChild(new Paragraph(new Run(new Text($"內容：{entry.Content}"))));
                                }
                            }

                            body.AppendChild(new Paragraph()); // 事件群組之間的空行
                        }
                    }



                    body.AppendChild(new Paragraph(new Run(new Text($"（一）延誤5分鐘以上事件{model.U5ControllableCountToday}件"))));
                    body.AppendChild(new Paragraph(new Run(new Text($"（二）延誤1分30秒～5分鐘事件{model.D5ControllableCountToday}件"))));
                    body.AppendChild(new Paragraph(new Run(new Text($"（三）延誤5分鐘以上不可抗力歸責事件{model.U5UncontrollableCountToday}件"))));
                    body.AppendChild(new Paragraph(new Run(new Text($"（四）延誤1分30秒～5分鐘不可抗力歸責事件{model.D5UncontrollableCountToday}件"))));


                    body.AppendChild(new Paragraph(new Run(new Text("三、急待解決事項"))));
                    if (string.IsNullOrWhiteSpace(model.Pending))
                    {
                        body.AppendChild(new Paragraph(new Run(new Text("無"))));
                    }
                    else
                    {
                        body.AppendChild(new Paragraph(new Run(new Text(model.Pending))));
                    }




                    body.AppendChild(new Paragraph(new Run(new Text("四、電聯車使用概況"))));
                    body.AppendChild(new Paragraph(new Run(new Text("（一）各時段使用列車數"))));


                    // 創建表格呈現高峰與離峰時段車輛數
                    Table peakPeriodsTable = new Table();
                    /// 建立標題列
                    TableRow headerRow = new TableRow();
                    headerRow.Append(
                        CreateTableCell("時段", true),
                        CreateTableCell("使用列車數", true)
                    );
                    peakPeriodsTable.Append(headerRow);

                    // 上午列
                    TableRow morningRow = new TableRow();
                    morningRow.Append(
                        CreateTableCell("上午尖峰/離峰", true),
                        CreateTableCell($"{model.MaxMorningPeakPeriod}/{model.MaxMorningOffPeakPeriod}")
                    );
                    peakPeriodsTable.Append(morningRow);

                    // 下午列
                    TableRow afternoonRow = new TableRow();
                    afternoonRow.Append(
                        CreateTableCell("下午尖峰/離峰", true),
                        CreateTableCell($"{model.MaxAfternoonPeakPeriod}/{model.MaxAfternoonOffPeakPeriod}")
                    );
                    peakPeriodsTable.Append(afternoonRow);



                    body.AppendChild(new Paragraph(new Run(new Text("（二）尖峰加班車使用列車數"))));




                    //四、（二）尖峰加班車使用列車數
                    // 創建表格呈現高峰與離峰時段車輛數
                    Table peakPeriodsWorkOverTimeTable = new Table();

                    // 設置表格屬性
                    TableProperties props = new TableProperties(
                        new TableBorders(
                            new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 1 },
                            new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 1 },
                            new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 1 },
                            new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 1 },
                            new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 1 },
                            new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 1 }
                        ),
                        new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
                    );
                    peakPeriodsWorkOverTimeTable.AppendChild(props);


                    // 創建標題行
                    TableRow WorkOverTimeheaderRow = new TableRow();
                    WorkOverTimeheaderRow.AppendChild(CreateTableCell("時段", true));
                    WorkOverTimeheaderRow.AppendChild(CreateTableCell("常態", true));
                    WorkOverTimeheaderRow.AppendChild(CreateTableCell("臨時", true));
                    peakPeriodsWorkOverTimeTable.AppendChild(WorkOverTimeheaderRow);

                    // 創建上午行
                    TableRow WorkOverTimemorningRow = new TableRow();
                    WorkOverTimemorningRow.AppendChild(CreateTableCell("上午", true));
                    WorkOverTimemorningRow.AppendChild(CreateTableCell(model.RegularMorningOvertimeWork ?? "-"));
                    WorkOverTimemorningRow.AppendChild(CreateTableCell(model.TemporaryMorningOvertimeWork ?? "-"));
                    peakPeriodsWorkOverTimeTable.AppendChild(WorkOverTimemorningRow);

                    // 創建下午行
                    TableRow WorkOverTimeafternoonRow = new TableRow();
                    WorkOverTimeafternoonRow.AppendChild(CreateTableCell("下午", true));
                    WorkOverTimeafternoonRow.AppendChild(CreateTableCell(model.RegularAfternoonOvertimeWork ?? "-"));
                    WorkOverTimeafternoonRow.AppendChild(CreateTableCell(model.TemporaryAfternoonOvertimeWork ?? "-"));
                    peakPeriodsWorkOverTimeTable.AppendChild(WorkOverTimeafternoonRow);

                    // 將表格添加至文件
                    body.AppendChild(peakPeriodsWorkOverTimeTable);

                    body.AppendChild(new Paragraph(new Run(new Text("（三）可用列車數"))));





                    //四、（三）可用列車數
                    // 建立表格
                    Table complexTable = new Table();
                    complexTable.AppendChild(new TableProperties(
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 4 },
                            new BottomBorder { Val = BorderValues.Single, Size = 4 },
                            new LeftBorder { Val = BorderValues.Single, Size = 4 },
                            new RightBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                        )
                    ));

                    // 第一列：車型、平常日（跨三欄）、例假日（跨兩欄）
                    TableRow row1 = new TableRow();
                    row1.Append(
                        CreateMergedCell("車型", 1, true),
                        CreateMergedCell("平常日", 3, true),
                        CreateMergedCell("例假日", 2, true)
                    );
                    complexTable.Append(row1);

                    // 第二列：平常日子欄（上午尖峰/下午尖峰/總數）、例假日子欄（尖峰/總數）
                    TableRow row2 = new TableRow();
                    row2.Append(
                        CreateEmptyCell(), // 車型（合併上方）
                        CreateTableCell("上午尖峰", true),
                        CreateTableCell("下午尖峰", true),
                        CreateTableCell("離峰", true),
                        CreateTableCell("尖峰", true),
                        CreateTableCell("離峰", true)
                    );
                    complexTable.Append(row2);

                    // 第三列：VAL256（範例數值可改為 model 裡的實際資料）
                    complexTable.Append(CreateTrainDataRow("VAL256", "7.1/2", "7.2/2", "7.3", "-", "7.3"));

                    // 第四列：BT370
                    complexTable.Append(CreateTrainDataRow("BT370", "7.4/2", "7.5/2", "7.6", "-", "7.6"));

                    // 第五列：合計
                    complexTable.Append(CreateTrainDataRow("合計", "7.1/2+7.4/2", "7.2/2+7.5/2", "7.3+7.6", "-", "7.3+7.6"));


                    // 插入表格至 Word
                    body.AppendChild(complexTable);













                    mainPart.Document.Append(body);
                    mainPart.Document.Save();
                }

                ms.Position = 0;
                return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "文湖線每日行車運轉狀況.docx");
            }
        }



        // 創建表格單元格的輔助方法
        private TableCell CreateTableCell(string text, bool isHeader = false)
        {
            TableCell cell = new TableCell();

            // 設置單元格屬性
            TableCellProperties cellProperties = new TableCellProperties();
            cellProperties.AppendChild(new TableCellWidth { Type = TableWidthUnitValues.Auto });

            // 如果是標題單元格，設置背景色和粗體文字
            if (isHeader)
            {
                cellProperties.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "D9D9D9" });
            }

            cell.AppendChild(cellProperties);

            // 創建段落和文字
            Paragraph p = new Paragraph();
            Run r = new Run();
            Text t = new Text(text);

            // 如果是標題單元格，設置粗體
            if (isHeader)
            {
                RunProperties runProperties = new RunProperties();
                runProperties.AppendChild(new Bold());
                r.AppendChild(runProperties);
            }

            // 將文字添加至段落並設置段落居中對齊
            r.AppendChild(t);
            p.AppendChild(r);
            p.AppendChild(new ParagraphProperties(
                new Justification() { Val = JustificationValues.Center }
            ));

            cell.AppendChild(p);
            return cell;
        }

        // 建立橫向合併的儲存格
        private TableCell CreateMergedCell(string text, int mergeCols, bool isHeader = false)
        {
            TableCell cell = CreateTableCell(text, isHeader);
            TableCellProperties props = cell.GetFirstChild<TableCellProperties>();
            props.Append(new GridSpan() { Val = mergeCols });
            return cell;
        }

        // 建立空儲存格（如車型欄上方合併需要）
        private TableCell CreateEmptyCell()
        {
            return CreateTableCell("");
        }

        // 建立資料列（車型 + 各數值）
        private TableRow CreateTrainDataRow(string type, string amPeak, string pmPeak, string total, string holidayPeak, string holidayTotal)
        {
            TableRow row = new TableRow();
            row.Append(
                CreateTableCell(type),
                CreateTableCell(amPeak),
                CreateTableCell(pmPeak),
                CreateTableCell(total),
                CreateTableCell(holidayPeak),
                CreateTableCell(holidayTotal)
            );
            return row;
        }




    }
}

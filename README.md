# TPMART
H.文湖線每日行車運轉狀況

WLineDailyOperationStatus=>Model

WLineDailyOperationStatusController=>Controller

WLineDailyOperationStatus=>View



有用到RP_EventDetails =>(U5Controllable等)

Preview.cshtml為預覽頁面



未建立資料庫儲存數值

未加上使用者輸入提示字元

目前預覽表格皆已建置完成，差需要計算之數據，Word匯出可正常匯出但還不完整，Index僅留需要輸入的資料


未完成部分：
四、（二） | 四、（三）


五、（一）=>8.6、8.10及C/A | 五、（二） | 五、（三） | 五、（四）


日期選擇=>僅查看當時資料（不確定）


======================================================



F. 故障統計表
KeyWordController.cs =>Controller

KeywordResultView,KeywordStatistic,KeywordStatisticPerDay =>Model

keyword =>View

KeywordStatistics =>SSMS


注意：
需下載ClosedXML套件=>匯出Excel
需下載Aspose.Words套件=>doc轉docx

===========================================================================

B. 列車正線運轉紀錄
TrainTransferRecordController=>Controller

TrainTransferRecord,Train=>Model

TrainTransferRecord=>View

TrainTransferRecords,Train=>SSMS

於資料庫中建立關聯性(TrainID)

用Google Charts製作甘特圖



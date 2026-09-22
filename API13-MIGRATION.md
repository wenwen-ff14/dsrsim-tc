# 繁中 API 13 移植

本機建置基準：Dalamud 13.0.0.16（FFXIVSimpleLauncher 隨附函式庫）、
Dalamud.NET.Sdk 13.0.0、.NET SDK 9.0.101、C# 13。

已調整 SDK 與套件鎖定檔、C# 14 語法、LINQ Shuffle、Dalamud 服務命名空間、
區域與副本事件回呼、Lumina Lockon 欄位、角色旗標、模型與 VFX 原生介面。
建置只從所選 SDK 目錄搜尋 Dalamud 相依組件，並拒絕 API 14 / 15。

原生相容處理的依據：

- 區域載入：Hyperborea `62d7003`，包含第六個參數。
- 副本事件：ECommons `cbc095d^` 的 `Hooks/DirectorUpdate.cs`，只接收四個 payload 參數。
- ActorControl：BossMod `07e42091f3b9951862d9c47344cfe874964ca457` 的
  `Framework/WorldStateGameSync.cs`，只接收六個 payload 參數，之後是 target ID 與 replay byte。
- 模型、VFX 與 SpawnObject 欄位：FFXIVClientStructs 定義及本機 API 13 DLL 比對。
  API 13 未命名的模型偏移 0x21 / 0x22、VFX 偏移 0x248 與 CleanupRender 虛擬函式
  仍需遊戲內驗證；簽章唯一匹配無法驗證這些欄位的語意。

事件物件查找改用 API 13 的 EventObjects 陣列；原先四個位元組的簽章在繁中
執行檔會匹配三處。原本半徑計算與事件物件狀態設定使用相同簽章但不同委派，
已移除前者，改用 SetupBNpc 解析的半徑，模型覆寫時使用 ModelChara 資料。

驗證結果：Debug / Release 建置成功，零警告、零錯誤；輸出 manifest 為
DalamudApiLevel 13。誤用本機 API 14.0.1.0 函式庫會被建置防護拒絕。
`tools/Check-NativeSignatures.ps1` 對本機繁中執行檔檢查的 29 個簽章皆唯一匹配。
此檢查只涵蓋專案字串常值中的簽章，不涵蓋 DLL 內部簽章、記憶體配置與執行行為。

尚未在遊戲內載入或執行。請於旅館透過 Dalamud Dev Plugins 載入 Release DLL，
檢查 `/ano`、隊友與敵人生成、讀條／特效／連線／狀態、重置，以及離開後回到旅館。
若載入或場景失敗，保留 Dalamud 記錄中的例外與堆疊。

CI 改為使用指定的繁中 API 13 SDK zip，產出 artifact；未執行遠端 CI 或發布。
絕龍詩 P2 聖杖 / tuuf 場景仍是後續工作，尚未包含於此移植。

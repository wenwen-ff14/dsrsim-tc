# 維護與發布

本文件供維護者使用；使用者安裝方式請見根目錄 README。

## 編譯

在專案根目錄執行：

```powershell
dotnet build AnoMech.sln -c Release -p:RestoreLockedMode=true
```

需要 .NET 9 與繁中 Dalamud API 13 SDK。SDK 預設位置為 `%APPDATA%/FFXIVSimpleLauncher/Dalamud/Injector`，亦可設定 `DALAMUD_HOME` 或 `-p:DalamudLibPath=...`。
輸出為 `AnoMech/bin/x64/Release/dsrsim-tc.dll`。原生動畫、特效及操作仍須在旅館內實機驗證。

## 發布

1. 調高 `AnoMech/AnoMech.csproj` 的四段版本號。
2. 在根目錄執行以下指令：

```powershell
pwsh -File tools/Prepare-PluginRepository.ps1 -Changelog '本次更新內容'
```

3. 檢查 `pluginmaster.json` 的版本、下載網址與 `packages/dsrsim-tc/<版本>.zip`。
4. 將原始碼、套件與插件庫索引一併提交並推送 main，網址安裝才會取得新版。

已發布套件須保留，不覆寫同版本 ZIP，以免破壞舊下載連結。簽章檢查與離線驗證工具位於 `tools/`；API 13 移植限制見 [API13-MIGRATION.md](API13-MIGRATION.md)。

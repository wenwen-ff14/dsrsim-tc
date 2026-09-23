# dsrsim-tc

![dsrsim-tc 圖示](images/dsrsim-tc.png)

繁中版 FF14 絕龍詩戰爭機制模擬器，使用 Dalamud API 13（.NET 9 / C# 13）。
本專案**參考並衍生自 AnoMech**（Anomek 與原專案貢獻者），沿用其本機模擬引擎，專注製作繁中版 DSR 練習內容，並非原版 AnoMech 的官方版本。

目前提供：

- **P2-聖仗**：分攤劍、雙視線、騎士衝鋒、白球、冰火圈、塔、隕石與擊退。
- **P3-尼德霍格**：可指定自己的麻將與箭頭；包含跳躍、分攤、內外圈、踩塔、引導槍、最後四塔、坦克接線、普攻與騰龍槍。
- **P4-雙眼**：思念增益、紅藍換線、雙人黃球、單人藍球及四輪幻象俯衝。雙眼擊破自動推進，尚未包含救奧爾什方。
- **P5-風槍**：FFLogs 校時；包含交叉衝鋒、旋風、八方劍、綠標龍衝、連續地火、雷光鏈、月環與古代爆震。詳見 [DSR-P5.md](DSR-P5.md)。
- **P5-死刻**：死亡宣告分組、沉重衝擊、百雷／衝鋒／旋風、PS 標記、雙視線、擊退斷鏈及白圈解死宣；目前練習至解死宣，不含隕石擊破與後續坦克連擊。詳見 [DSR-P5-Death.md](DSR-P5-Death.md)。

打法參考 tuuf／Elemental 的 [P2](https://ffxiv.tuufless.com/elemental/dsr/02_thordan/)、[P3](https://ffxiv.tuufless.com/elemental/dsr/03_nidhogg/) 、[P4](https://ffxiv.tuufless.com/elemental/dsr/04_eyes/) 與 [P5](https://ffxiv.tuufless.com/elemental/dsr/05_alternate_thordan/) 攻略。

## 安裝

在 Dalamud 設定的「自訂插件庫／Custom Plugin Repositories」新增並啟用：

```text
https://raw.githubusercontent.com/wenwen-ff14/test/main/pluginmaster.json
```

於插件安裝器搜尋 **dsrsim-tc** 並安裝。

自 0.4.0.36 起，InternalName、DLL 與更新套件皆改為 `dsrsim-tc`，設定與日誌使用獨立的插件目錄，視窗識別也與 AnoMech 分離。指令為 `/dsrsim` 或 `/dsrsim-tc`，不再註冊 `/ano`、`/anomech`。

**從本分支舊版遷移：**請先停用原先的「絕龍詩模擬器」（InternalName 為 AnoMech），再安裝 dsrsim-tc；Dalamud 會將新識別視為另一個插件，舊版不會自動改名，設定也不會自動搬移。若用 Dev Plugins，請改載入 `dsrsim-tc.dll`。原版 AnoMech 可以保留，但兩個模擬器請勿同時啟動場景；兩者仍會操作同一個遊戲場地與原生物件。

## 使用

1. 進入旅館房間，輸入 `/dsrsim`。
2. 選擇關卡與職責，按「開始」；七名 NPC 依 tuuf 分工走位，玩家自行操作。
3. 可開啟無敵練習、站位提示與戰術圖。P3 麻將及箭頭選項於下一輪生效。
4. P4 換線需實際接觸；NPC 等交換成功才離開，雙方有三秒不可再換線的減益。
5. 指令支援 `config`、`start`、`reset`、`leave`，例如 `/dsrsim reset`。
6. 「離開模擬」返回旅館。機制中可收合視窗；各階段每輪重新隨機，P5 可分組指定玩家點名。

P2 外圈擊退需使用親疏自行／沉穩詠唱，並啟用「模擬自身技能效果」。P5 死刻需要正常吃擊退，不能開防擊退。配樂使用遊戲 BGM 音量。
所有關卡保留背景音樂開關，不再顯示重新播放音樂按鈕。

模擬時會暫時隔離伺服器封包，因此隊伍加入／離開與準備確認等更新可能無法正常顯示，直到離開模擬。
原生動畫、特效、場地與 BGM 仍需在繁中遊戲內驗收；離線測試與編譯不能替代實機確認。
機制範圍與時序限制詳見 [P2](DSR-P2.md)、[P3](DSR-P3.md)、[P4](DSR-P4.md)。

## 建置與發布

```powershell
dotnet build AnoMech.sln -c Release -p:RestoreLockedMode=true
```

保留原始碼目錄與 solution 名稱以減少無關搬移；輸出的插件識別為 `dsrsim-tc`。
SDK 優先使用 `%APPDATA%/FFXIVSimpleLauncher/Dalamud/Injector`，亦可設定 `DALAMUD_HOME` 或 `-p:DalamudLibPath=...`；建置會檢查 API 13。

- 本機 DLL：`AnoMech/bin/x64/Release/dsrsim-tc.dll`
- 打包輸出：`AnoMech/bin/x64/Release/dsrsim-tc/latest.zip`
- 插件庫套件：`packages/dsrsim-tc/<版本>.zip`

調高 csproj 的四段版本號後，執行：

```powershell
pwsh -File tools/Prepare-PluginRepository.ps1 -Changelog '本次更新內容'
```

將原始碼、套件與 `pluginmaster.json` 一併提交並推送。已發布的舊版套件保留，不覆寫。
GitHub Actions 需指定繁中 API 13 SDK，設定方式見工作流程與 [API13-MIGRATION.md](API13-MIGRATION.md)。

## 來源、授權與致謝

- **AnoMech / Anomek**：本專案的原始碼與模擬引擎來源；[原作者插件庫](https://github.com/anomek/MyDalamudPlugins)。
- **WorstAquaPlayer、Wydox、RoarkGit**：原版引擎、機制及修正貢獻。
- **tuuf／Elemental**：DSR 攻略與職責分工。
- **Hyperborea、FFXIV-RaidsRewritten、BossMod**：原專案採用的場地載入、戰鬥特效及機制時序參考。

保留原版著作權與 [AGPL-3.0-or-later 授權](LICENSE.md)。本分支由 wenwen-ff14 維護。
新圖示為本專案生成的龍眼與騎士主題圖案，並非遊戲官方圖示。

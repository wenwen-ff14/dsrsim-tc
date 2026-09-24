# P2 騎士動畫資源核對

核對日期：2026-09-24。來源為本機繁中客戶端 SqPack 的 BNpcBase、ModelChara、NpcEquip、ActionTimeline，以及 TMB 引用與 PAP 動畫名稱。不代表已在遊戲內確認視覺，也尚未證實哪一組對應實戰影片的指定時刻。

## 模型與武器分類

目前場景使用的八名騎士 BNpcBase（3139、3158、3130、313A、315C、313B、315E、315F，十六進位）皆指向 ModelChara 3268：demihuman d1054，base 1、variant 1。裝備不同；相同 timeline 可依武器分類選到不同動畫。

下表列出實際存在於 `chara/demihuman/d1054/animation/a0001/` 的資源；「—」表示本次沒有在該分類的 resident PAP 中找到對應動畫，不代表引擎一定沒有其他替代路徑。

| 武器分類 | 戰鬥待機 34 | A 起手 130／循環 131 | B 起手 132／循環 133 | C 循環 135 | 普攻 112 |
|---|---|---|---|---|---|
| 劍盾 `bt_swd_sld` | 有 | —／— | 有／有 | — | 有 |
| 大劍 `bt_2sw_emp` | 有 | 有／有 | 有／有 | — | 有 |
| 雙手斧 `bt_2ax_emp` | 有 | —／有 | —／有 | 有 | 有 |
| 長槍 `bt_2sp_emp` | 有 | —／有 | —／— | — | 有 |
| 雙手杖 `bt_2st_emp` | 有 | 有／有 | 有／有 | — | 有 |

## 可核對到的 timeline

| ID（十進位） | TMB key | PAP 動畫名稱 | 用途／限制 |
|---|---|---|---|
| 34 | `battle/idle` | `cbbm_id0` | 各武器分類有獨立戰鬥待機資源 |
| 130 → 131 | `battle/mon_sp_a_start` → `battle/mon_sp_a_loop` | `cbbm_sp_a_1` → `cbbm_sp_a_2lp` | A 組起手接循環；實際姿態待確認 |
| 132 → 133 | `battle/mon_sp_b_start` → `battle/mon_sp_b_loop` | `cbbm_sp_b_1` → `cbbm_sp_b_2lp` | B 組起手接循環；劍盾騎士的優先試播候選 |
| 135 | `battle/mon_sp_c_loop` | `cbbm_sp_c_2lp` | 斧類另有 C 組循環 |
| 210 | `idle_sp/idle_sp_1` | `cbbm_idle_sp1` | `bt_common` 有特殊待機資源；適用外觀與循環行為待確認 |
| 112／121／124 | `battle/auto_attack1`／`battle/auto_attack1_mon_a`／`battle/auto_attack1_mon_b` | `cbbm_atk1` | 三個 TMB 引用同名普攻動畫，不能當成三套不同姿勢 |
| 35／36 | `battle/turn_loop_l`／`battle/turn_loop_r` | `cbbm_trn_l_lp`／`cbbm_trn_r_lp` | 戰鬥轉身 |
| 50 | `battle/run` | `cbbm_02f_lp0` | 戰鬥跑動 |
| 7737（0x1E39） | `warp/warp_start` | `cbbm_warp_1` | 消失／上天過場；140、4609 也指向相同 key |
| 7747（0x1E43） | `warp/warp_end` | `cbbm_warp_3` | 出現／落地過場；141、4610 也指向相同 key |
| 72／73 | `battle/dead`／`battle/dead_pose` | `cbbm_ded`／`cbbm_dedpose` | 死亡動作／姿態 |

## 對 P2 的意義

- 兩名劍盾騎士優先比較 34 與 132 → 133；黑騎大劍另可比較 130 → 131。A、B 名稱只是資源分組，不能直接翻譯成「蓄力」「防禦」或「舉劍」。
- 若需要持續動作，應播放對應循環資源，而非延長一次性落地或普攻的保持時間。起手切換循環的時間與動作幅度仍需實機確認。
- `bt_common/resident/idle.pap` 在此模型是零動畫的空容器；各武器分類的 idle PAP 才含 `cbbm_id0`。應核對武器姿態與資源選擇，但不能僅憑空容器認定目前僵住的原因。
- 泛用資料表中的舉盾、武器格擋、各職業詠唱名稱，未在本次騎士 PAP 核對中取得對應證據，不列為已確認可用的騎士姿勢。
- 本次 Action sheet 未成功由現有 Lumina schema 讀取，因此沒有新增對技能 ID 與這些 A／B／C 組動畫的對應結論。

後續試播版本已將 P2 雙劍盾騎士設定為 132 → 133、黑騎設定為 130 → 131，等待實機驗證。原始核對輸出位於本機忽略追蹤的 `tools/reference/knight-animation-audit.txt`，調查程式位於 `tools/reference/KnightInspect/`。

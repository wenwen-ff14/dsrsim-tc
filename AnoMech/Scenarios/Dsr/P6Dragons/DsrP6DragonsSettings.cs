using Dalamud.Bindings.ImGui;
using AnoMech.Core;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

public sealed partial class DsrP6DragonsScenario
{
    public void DrawSettings()
    {
        var first=(int)firstSection-1;var last=(int)lastSection-1;
        string[] sections=["冰火 1（含分攤）","第一次坦死刑","十字火（含分攤）","第二次坦死刑","冰火 2（固定式、雙龍俯衝）"];
        if(ImGui.Combo("開始機制",ref first,sections,sections.Length))SetRange((DsrP6Section)(first+1),(DsrP6Section)(System.Math.Max(first,last)+1));
        if(ImGui.Combo("結束機制",ref last,sections,sections.Length))SetRange((DsrP6Section)(System.Math.Min((int)firstSection-1,last)+1),(DsrP6Section)(last+1));
        ImGui.TextWrapped($"下次開始：{SectionName(firstSection)} → {SectionName(lastSection)}");
        ImGui.Combo("十字火標記",ref markingMode,["系統標記","玩家手標"],2);
        ImGui.Combo("散攤走法",ref spreadStrategy,["黑找黑、白找白","左散右攤（面向場中）"],2);
        ImGui.TextWrapped("散開：攻擊 1～4；分攤：鎖鏈 1～2；無點名：禁止 1～2。同號鎖鏈與禁止配對，依 123412 排列。設定下次開始生效。");
        ImGui.TextWrapped("十字火：中央先出、後兩組對角隨機；白龍西／中／東及南／北隨機。等命中特效後走 L 型，黑龍側可繞目標圈。火球時間與位置仍待原始 ACT／影片精校。");
        ImGui.TextWrapped("結尾隨機翼／尾：翼躲中央窄帶，尾避開中央；玩家手標時，NPC 等待八個正確標記後就位。");
        ImGui.TextWrapped("冰火 2 使用中文攻略固定式：冰火連線隨機，職責站位固定；單龍發光為另一側坦克單吃，雙龍發光為雙坦場中分攤。冰火 1 與翅膀死刑仍使用指定紀錄分支。坦克減傷與雙龍血量差尚未模擬。");
        var music=!Plugin.Config.SuppressBgm;
        if(ImGui.Checkbox("背景音樂：Dragonsong",ref music)){Plugin.Config.SuppressBgm=!music;Plugin.Config.Save();}
        if(state==null)return;
        if(!state.SystemMarks&&state.FlamesAssigned&&!state.Complete&&state.Time<86.212f&&ImGui.TreeNode("手動設定頭標"))
        {
            string[] roles=["未標記","MT","ST","H1","H2","D1","D2","D3","D4"];
            string[] labels=["攻擊 1","攻擊 2","攻擊 3","攻擊 4","鎖鏈 1","鎖鏈 2","禁止 1","禁止 2"];
            for(var i=0;i<FlameSigns.Length;i++)
            {
                var selected=0;
                for(var r=0;r<8;r++)if(world!.Party.Get(r) is {} member&&Markings.IsSetOn(FlameSigns[i],member.GameObjectId))selected=r+1;
                if(ImGui.Combo(labels[i],ref selected,roles,roles.Length))
                {
                    Markings.Clear(FlameSigns[i]);
                    if(selected>0&&world!.Party.Get(selected-1) is {} member)
                    {
                        foreach(var sign in FlameSigns)if(Markings.IsSetOn(sign,member.GameObjectId))Markings.Clear(sign);
                        Markings.Set(FlameSigns[i],member.GameObjectId);
                    }
                }
            }
            ImGui.TreePop();
        }
        ImGui.TextUnformatted($"P6　{state.Time:F1} 秒");
        ImGui.TextWrapped(state.Complete?(state.Failed?"本輪有失誤，可重置重練。":"本段完成。"):state.Hint);
    }
}

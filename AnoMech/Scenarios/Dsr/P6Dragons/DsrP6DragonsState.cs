using System;
using System.Linq;
using System.Numerics;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

public enum DsrP6Section { Full, Breath1, Wings1, Wroth, Wings2, Breath2 }
internal enum DsrP6Glow { Nidhogg, Hraesvelgr, Both }

internal sealed class DsrP6DragonsState
{
    public static readonly Vector3 Nidhogg=new(-22,0,0), Hraesvelgr=new(22,0,0);
    private static readonly Vector3[] FixedBreathPositions=
    [
        new(-14.8f,0,-12.7f),new(14.8f,0,12.7f),
        new(-1.4f,0,-19.6f),new(1.4f,0,19.6f),
        new(-3.8f,0,9.6f),new(3.8f,0,-9.6f),
        new(-7.4f,0,18.1f),new(7.4f,0,-18.1f),
    ];
    public readonly Vector3[] Destinations=new Vector3[8];
    public readonly Vector3?[] LastDestinations=new Vector3?[8];
    public readonly bool[] Fire=new bool[8];
    public readonly bool[] SecondFire=new bool[8];
    public DsrP6Glow SecondGlow;
    public DsrP6WingsPattern FirstWings, SecondWings;
    public readonly int[] Flames;
    public readonly int FirstVow;
    public readonly int AkhMornTarget;
    public DsrP6WrothPattern Wroth;
    public bool WrothHotWing, SystemMarks=true, RelativeSpread, FlamePositioning;
    public int[] FlameOrder;
    public int VowOwner=-1;
    public bool Complete, Failed, SecondBreath, FlamesAssigned, ThermalActive;
    public float Time;
    public string Hint="雙龍登場，準備冰火。";
    public DsrP6DragonsState(int seed)
    {
        var random=new Random(seed);
        FirstVow=random.Next(4,8);
        Flames=Enumerable.Range(0,8).ToArray();random.Shuffle(Flames);
        FlameOrder=(int[])Flames.Clone();
        var breathRoles=Enumerable.Range(2,6).ToArray();random.Shuffle(breathRoles);
        foreach(var role in breathRoles.Take(3))SecondFire[role]=true;
        SecondGlow=(DsrP6Glow)random.Next(3);
        AkhMornTarget=random.Next(8);
        Wroth=new(random.Next(24));
        WrothHotWing=random.Next(2)==0;
        FirstWings=new(random.Next(2)==0,random.Next(2)==0,random.Next(2)==0,random.Next(2)==0,true);
        SecondWings=new(random.Next(2)==0,random.Next(2)==0,false,false,random.Next(2)==0);
        SetIdle();
    }
    public void SetIdle()
    {
        for(var r=0;r<8;r++)Destinations[r]=new((r%2==0?-1:1)*(r<2?16:8),0,(r/2-1)*4);
    }
    public void SetBreath(bool second)
    {
        SecondBreath=second;
        Destinations[0]=new(-14,0,second?12:-12);Destinations[1]=second?new(14,0,12):new(15,0,-13);
        if(!second)
        {
            int[][] pairs=[[2,4],[3,5],[7,6]];
            Vector3[] anchors=[new(-6,0,18),new(6,0,18),new(0,0,8)];
            for(var i=0;i<3;i++)
            {
                Fire[pairs[i][0]]=true;Fire[pairs[i][1]]=false;
                Destinations[pairs[i][0]]=anchors[i];
                Destinations[pairs[i][1]]=anchors[i]+new Vector3(0,0,.6f);
            }
        }
        else
        {
            SecondFire.CopyTo(Fire,0);
            FixedBreathPositions.CopyTo(Destinations,0);
            if(SecondGlow==DsrP6Glow.Both)Destinations[0]=Destinations[1]=Vector3.Zero;
        }
        Hint=second?"固定式冰火：H1 北略偏西／H2 南略偏東，D2／D4 東北，D1／D3 西南；D3／D4 在直列地磚外側、暴雪環內。雙龍發光時雙坦場中分攤，否則 MT 西北、ST 東南。":"冰火一：兩人一組，讓每人同時受到冰與火；ST 遠離人群承受單坦死刑。";
    }
    public void SetStacks()
    {
        for(var r=0;r<8;r++)Destinations[r]=new(0,0,r%2==0?-6:6);
        Hint="無盡輪迴：MT／H1／D1／D3 北側，ST／H2／D2／D4 南側，四人分攤。";
    }
    public void SetVowSpread()
    {
        for(var r=0;r<8;r++)Destinations[r]=new(r%2==0?-10:10,0,-12+r/2*8);
        Hint="DPS 分散，等待滅殺的誓言點名後再集合分攤。";
    }
    public void SetWings(bool second,bool hotKnown=true)
    {
        var pattern=second?SecondWings:FirstWings;
        for(var r=0;r<8;r++)Destinations[r]=pattern.Position(r,second,hotKnown);
        Hint=(pattern.SouthCleave?"北半安全；":"南半安全；")+
            (pattern.Far?"抬頭：雙坦最遠，人群靠白龍。":"低頭：雙坦最近，人群遠離白龍。")+
            (second?(hotKnown?(pattern.HotWing?"翼：中央窄帶。":"尾：遠離中央線。") :"等待黑龍翼／尾讀條。")+
                "MT 靠東／西場邊、ST 靠場中。":(pattern.DiveWest?"避開西半俯衝。":"避開東半俯衝。")+"MT 靠南／北場邊、ST 靠場中。");
    }
    public void SetVowPass(int receiver,bool preserveOthers=false)
    {
        if(!preserveOthers)SetIdle();
        else for(var r=0;r<8;r++)
            if(r!=VowOwner&&r!=receiver&&Destinations[r].LengthSquared()<49)
                Destinations[r]=new(Destinations[r].X,0,-Wroth.StartZ*8);
        if(VowOwner>=0)Destinations[VowOwner]=Vector3.Zero;
        Destinations[receiver]=Vector3.Zero;
        Hint="滅殺的誓言：持有者與接毒者在場中重疊，其餘人遠離。";
    }
    public void SetFlames()
    {
        var mirror=RelativeSpread?Wroth.StartZ:1;
        var z=-Wroth.StartZ*(WrothHotWing?2:10);
        for(var i=0;i<4;i++)Destinations[FlameOrder[i]]=new(mirror*(-18+i*6),0,z);
        for(var i=4;i<8;i++)Destinations[FlameOrder[i]]=new(mirror*(i%2==0?9:18),0,z);
        Hint=(WrothHotWing?"翼：靠中央窄帶。":"尾：遠離中央橫線。")+(RelativeSpread?"面向場中左散右攤，依 123412 排列。":"黑找黑、白找白，依 123412 排列。");
    }
    public void SetDoubleDive()
    {
        Destinations[0]=new(-10,0,-18);Destinations[1]=new(10,0,-18);
        for(var r=2;r<8;r++)Destinations[r]=new(Fire[r]?10:-10,0,-8+r);
        Hint="雙龍俯衝：雙坦站最北承受首擊；火去白龍、冰去黑龍，站定等俯衝。";
    }
}

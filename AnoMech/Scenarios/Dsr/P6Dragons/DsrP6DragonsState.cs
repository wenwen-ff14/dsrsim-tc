using System;
using System.Linq;
using System.Numerics;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

public enum DsrP6Section { Full, Breath1, Wings1, Wroth, Wings2, Breath2 }

internal sealed class DsrP6DragonsState
{
    public static readonly Vector3 Nidhogg=new(-22,0,0), Hraesvelgr=new(22,0,0);
    public readonly Vector3[] Destinations=new Vector3[8];
    public readonly Vector3?[] LastDestinations=new Vector3?[8];
    public readonly bool[] Fire=new bool[8];
    public readonly int[] Flames;
    public readonly int FirstVow;
    public int VowOwner=-1;
    public bool Complete, Failed, SecondBreath, FlamesAssigned, ThermalActive;
    public float Time;
    public string Hint="雙龍登場，準備冰火。";
    public DsrP6DragonsState(int seed)
    {
        var random=new Random(seed);
        FirstVow=random.Next(4,8);
        Flames=Enumerable.Range(0,8).ToArray();random.Shuffle(Flames);
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
            Vector3[] anchors=[new(-8,0,9),new(8,0,9),new(0,0,-8)];
            for(var i=0;i<3;i++)
            {
                Fire[pairs[i][0]]=true;Fire[pairs[i][1]]=false;
                Destinations[pairs[i][0]]=anchors[i];
                Destinations[pairs[i][1]]=anchors[i]+new Vector3(0,0,.6f);
            }
        }
        else
        {
            int[] fire=[2,4,7],ice=[3,5,6];
            Vector3[] fireSpots=[new(0,0,-19),new(8,0,-17),new(14,0,-12)];
            Vector3[] iceSpots=[new(-14,0,-12),new(-9,0,-17),new(-6,0,18)];
            for(var i=0;i<3;i++)
            {
                Fire[fire[i]]=true;Fire[ice[i]]=false;
                Destinations[fire[i]]=fireSpots[i];Destinations[ice[i]]=iceSpots[i];
            }
        }
        Hint=second?"固定式冰火：依固定站位散開，保留屬性給雙龍俯衝消除。":"冰火一：兩人一組，讓每人同時受到冰與火；ST 遠離人群承受單坦死刑。";
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
    public void SetWings(bool second)
    {
        if(!second)
        {
            Destinations[0]=new(-18,0,18);Destinations[1]=new(-18,0,5);
            for(var r=2;r<8;r++)Destinations[r]=new(-2,0,9);
        }
        else
        {
            Destinations[0]=new(-18,0,-6);Destinations[1]=new(-4,0,-6);
            for(var r=2;r<8;r++)Destinations[r]=new(15,0,-6);
        }
        Hint=second?"第二次坦死刑：躲南半場與燃燒之翼；雙坦保持最遠並彼此拉開。":"第一次坦死刑：躲北半場與東半場俯衝；雙坦保持最遠並彼此拉開。";
    }
    public void SetVowPass(int receiver)
    {
        SetIdle();
        if(VowOwner>=0)Destinations[VowOwner]=Vector3.Zero;
        Destinations[receiver]=Vector3.Zero;
        Hint="滅殺的誓言：持有者與接毒者在場中重疊，其餘人遠離。";
    }
    public void SetFlames()
    {
        for(var i=0;i<4;i++)Destinations[Flames[i]]=new(-18+i*6,0,10);
        for(var i=4;i<8;i++)Destinations[Flames[i]]=new(i%2==0?9:18,0,10);
        Hint="燃燒之尾：離開中央橫線；黑色散開往西，白色與無標各二人分攤往東。";
    }
    public void SetDoubleDive()
    {
        Destinations[0]=new(-10,0,-18);Destinations[1]=new(10,0,-18);
        for(var r=2;r<8;r++)Destinations[r]=new(Fire[r]?10:-10,0,-8+r);
        Hint="雙龍俯衝：雙坦站最北承受首擊；火去白龍、冰去黑龍，站定等俯衝。";
    }
}

using System;
using System.Numerics;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

internal sealed class DsrP7DragonKingState
{
    public readonly Random Random;
    public readonly Vector3[] Destinations=new Vector3[8];
    public readonly Vector3[] LastDestinations=new Vector3[8];
    public readonly bool[] Sacrificed=new bool[8];
    public readonly int[] DarkStacks=new int[8],LightStacks=new int[8];
    public readonly float[] DarkUntil=new float[8],LightUntil=new float[8],PhysicalUntil=new float[8];
    public readonly Vector3[] Exaflares=new Vector3[3],Towers=new Vector3[3],Gigaflares=new Vector3[3];
    public readonly float[] ExaflareRotations=new float[3];
    public bool Fire,Complete,Failed,ManualTanks,FaceTank=true,Enrage;
    public bool AlignForExaflares;
    public int TankSide=-1;
    public int TowerPlan,TowerRound;
    public bool SoloTowers=>TowerPlan==1&&TowerRound==3||TowerPlan==2&&TowerRound>=2;
    public int BlueTowerTank=>TowerPlan==2&&TowerRound==3?1:0;
    public int MainTank,ExpectedTank,TrinityRole=4,TrinityHits,MechanicIndex,SwordChoice;
    public float Time,Facing=MathF.PI,MechanicFacing=MathF.PI;
    public string Hint="讓一仇坦克把托爾丹面向北方或南方。";
    public string TankMessage="";
    public DsrP7DragonKingState(int seed)
    {
        Random=new(seed);
        Array.Fill(LastDestinations,new Vector3(float.NaN));
        SetTrinity(4);
        Destinations[0]=new(0,0,-6);
        Destinations[1]=new(0,0,-10);
    }
    public Vector3 Relative(Vector3 p)=>Rotate(p,MechanicFacing-MathF.PI);
    public static Vector3 Rotate(Vector3 p,float angle)=>new(p.X*MathF.Cos(angle)+p.Z*MathF.Sin(angle),0,p.Z*MathF.Cos(angle)-p.X*MathF.Sin(angle));
    public static Vector3 Radial(float angle,float radius)=>new(MathF.Sin(angle)*radius,0,MathF.Cos(angle)*radius);
    public void SetAll(Vector3 p)
    {
        for(var r=0;r<8;r++)if(!Sacrificed[r])Destinations[r]=p;
    }
    public void SetTrinity(int role)
    {
        TrinityRole=role;FaceTank=true;
        for(var r=2;r<8;r++)Destinations[r]=Relative(new((r%2==0?-1:1)*3.5f,0,6));
        Destinations[role]=Relative(new(0,0,1.5f));
        SetTankPositions();
        Hint=$"三劍一體：{RoleName(role)} 進目標圈；兩坦與人群保持 3 碼間距。";
        if(AlignForExaflares)Hint+=" 下一輪地火：雙坦同側靠 A 或 C，接手一仇後保持南北面向。";
    }
    public void SetTankPositions()
    {
        if(AlignForExaflares)
        {
            Destinations[0]=new(0,0,TankSide*6);
            Destinations[1]=new(0,0,TankSide*10);
            return;
        }
        Destinations[0]=Relative(new(0,0,-6));
        Destinations[1]=Relative(new(0,0,-10));
    }
    public void SetTowerPositions(bool inner)
    {
        for(var r=0;r<8;r++)
        {
            var tower=TowerForRole(r);
            var p=Towers[tower];
            Destinations[r]=Vector3.Normalize(p)*(inner?6.2f:Fire?9.2f:6.5f);
        }
    }
    public int TowerForRole(int role)=>SoloTowers?(role>=2?0:role==BlueTowerTank?2:1):role<2?2:role%2;
    public void ExpireStatuses()
    {
        for(var r=0;r<8;r++)
        {
            if(Time>=DarkUntil[r])DarkStacks[r]=0;
            if(Time>=LightUntil[r])LightStacks[r]=0;
        }
    }
    public static string RoleName(int r)=>r switch{0=>"MT",1=>"ST",2=>"H1",3=>"H2",4=>"D1",5=>"D2",6=>"D3",_=>"D4"};
}

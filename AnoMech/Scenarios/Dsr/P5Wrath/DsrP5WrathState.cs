using System;
using System.Linq;
using System.Numerics;
namespace AnoMech.Scenarios.Dsr.P5Wrath;
internal sealed class DsrP5WrathState
{
    public int Seed { get; }
    public readonly float Rotation;
    public readonly int Blue;
    public readonly int[] TetherRoles;
    public readonly int[] EastRoles;
    public readonly bool GrinnauxNorth;
    public readonly Vector3[] Twisters = new Vector3[8];
    public readonly Vector3?[] Destinations = new Vector3?[8];
    public bool Assigned, ChargesResolved, TwistersActive, Failed, Complete;
    public float Time;
    public int Green, Liquid = -1, Altar;
    public int[] Thunder = [];
    public bool GreenAssigned, MercyLocked, MercyResolved, DiveLocked, FinaleResolved;
    public Vector3 DiveTarget;
    public readonly Vector3[] MercyTargets = new Vector3[8];
    public DsrP5WrathState(int seed, int playerRole = -1, int practiceTarget = 0, int followupTarget = 0)
    {
        Seed = seed;
        var random = new Random(seed);
        Rotation = random.Next(4) * MathF.PI / 2;
        GrinnauxNorth = random.Next(2) == 0;
        if(practiceTarget is < 0 or > 4 || followupTarget is < 0 or > 3 || (practiceTarget != 0 && followupTarget == 2))
            throw new ArgumentException("五火需搭配第一組隨機，不能與放大圈、連線或龍衝引導重疊。");
        var roles = Enumerable.Range(0,8).ToArray();
        int[] candidatesForPlayer;
        do
        {
            Liquid = -1;
            random.Shuffle(roles);
            Blue = roles[0];
            TetherRoles = [roles[1],roles[2]];
            EastRoles = roles.Skip(3).Order().ToArray();
            Green = EastRoles[random.Next(5)];
            Altar = new[]{Blue,TetherRoles[0],TetherRoles[1],Green}[random.Next(4)];
            var candidates = roles.Where(r => r != Altar).ToArray();
            random.Shuffle(candidates);
            Thunder = candidates.Take(2).ToArray();
            Liquid = EastRoles.Where(r => r != Green && !Thunder.Contains(r)).OrderByDescending(r => Vector3.Distance(SpreadPosition(r), Grinnaux)).First();
            if(playerRole is < 0 or > 7 || (practiceTarget == 0 && followupTarget == 0)) return;
            candidatesForPlayer = roles.Where(r => (practiceTarget switch
            {
                0 => true,
                1 => r == Blue,
                2 => r == TetherRoles[0],
                3 => r == TetherRoles[1],
                4 => r == Green,
                _ => false,
            }) && (followupTarget switch
            {
                0 => true,
                1 => Thunder.Contains(r),
                2 => r == Liquid,
                3 => r == Altar,
                _ => false,
            })).ToArray();
        } while(candidatesForPlayer.Length == 0);
        var selected = candidatesForPlayer[random.Next(candidatesForPlayer.Length)];
        int Remap(int role) => role == selected ? playerRole : role == playerRole ? selected : role;
        Blue = Remap(Blue);
        TetherRoles = TetherRoles.Select(Remap).ToArray();
        EastRoles = EastRoles.Select(Remap).ToArray();
        Green = Remap(Green);
        Liquid = Remap(Liquid);
        Altar = Remap(Altar);
        Thunder = Thunder.Select(Remap).ToArray();
    }
    public Vector3 SpreadPosition(int role)
    {
        var farRole=GrinnauxNorth?TetherRoles[0]:Blue;
        if(Altar!=Green)
        {
            if(role==Altar) role=farRole;
            else if(role==farRole) role=Altar;
        }
        float angle;
        if(role == Green) angle = GrinnauxNorth ? 180 : 0;
        else if(role == Blue) angle = GrinnauxNorth ? 315 : 290;
        else if(role == TetherRoles[0]) angle = GrinnauxNorth ? 225 : 210;
        else if(role == TetherRoles[1]) angle = GrinnauxNorth ? 270 : 250;
        else
        {
            var east=EastRoles.Where(r=>r!=Green).OrderBy(r=>r==Liquid?(GrinnauxNorth?1:-1):0).ToArray();
            angle = (GrinnauxNorth ? 30 : 40) + 40 * Array.IndexOf(east,role);
        }
        return Rotate(new(19.5f*MathF.Sin(angle*MathF.PI/180),0,-19.5f*MathF.Cos(angle*MathF.PI/180)));
    }
    public Vector3 SafePosition(int role)
    {
        var index = Array.IndexOf(Thunder,role);
        return Grinnaux + Rotate(index >= 0 ? new(index==0?-3.8f:3.8f,0,GrinnauxNorth?-2.8f:2.8f) : new(0,0,GrinnauxNorth?3: -3));
    }
    public Vector3 BaitDestination(int role)
    {
        var sign=GrinnauxNorth?1:-1;
        if(role==Liquid)
        {
            if(Time>29.92f) return SafePosition(role);
            var angle=(GrinnauxNorth?150:140)*MathF.PI/180-(MathF.Max(0,Time-25.176f)*6+.2f)/19.5f;
            return Rotate(new(19.5f*MathF.Sin(angle),0,-19.5f*MathF.Cos(angle)*sign));
        }
        var side=role==Liquid?1:-1;
        Vector3[] path=role==Green
            ? [SpreadPosition(role),Rotate(new(-14,0,0)),SafePosition(role)]
            : [SpreadPosition(role),Rotate(new(18*side,0,10*sign)),Rotate(new(15*side,0,-9*sign)),SafePosition(role)];
        var distance=MathF.Max(0,Time-(role==Green?26.07f:25.176f))*6+.2f;
        for(var i=1;i<path.Length;i++)
        {
            var length=Vector3.Distance(path[i-1],path[i]);
            if(distance<length) return Vector3.Lerp(path[i-1],path[i],distance/length);
            distance-=length;
        }
        return path[^1];
    }
    public Vector3 Rotate(Vector3 p) => new(p.X*MathF.Cos(Rotation)-p.Z*MathF.Sin(Rotation),p.Y,p.X*MathF.Sin(Rotation)+p.Z*MathF.Cos(Rotation));
    public Vector3 InitialPosition(int role)
    {
        if(role == Blue) return Rotate(new(-16,0,-11));
        if(role == TetherRoles[0]) return Rotate(new(6,0,18.8f));
        if(role == TetherRoles[1]) return Rotate(new(-6,0,18.8f));
        var angle = (56 + Array.IndexOf(EastRoles,role)*17) * MathF.PI / 180;
        return Rotate(new(19.5f*MathF.Sin(angle),0,-19.5f*MathF.Cos(angle)));
    }
    public Vector3 DodgePosition(int role)
    {
        var p = InitialPosition(role);
        if(Array.IndexOf(EastRoles,role)>=0) return Vector3.Normalize(p)*15;
        var angle = 15*MathF.PI/180;
        return new(p.X*MathF.Cos(angle)-p.Z*MathF.Sin(angle),0,p.X*MathF.Sin(angle)+p.Z*MathF.Cos(angle));
    }
    public Vector3 KnightPosition(int index) => Rotate(new(index==0 ? -10 : 10,0,-21));
    public Vector3 WhiteDragon => Rotate(new(0,0,-24));
    public Vector3 Grinnaux => Rotate(new(0,0,GrinnauxNorth ? -12 : 12));
    public Vector3 Charibert => -Grinnaux;
}

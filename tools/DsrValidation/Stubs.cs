using System.Numerics;

namespace AnoMech.Core
{
    public enum Sign { Attack1,Attack2,Attack3,Attack4,Bind1,Bind2,Ignore1,Ignore2 }
    internal static class Markings
    {
        public static readonly Dictionary<Sign,uint> Values=[];
        public static void Set(Sign sign,uint target)=>Values[sign]=target;
        public static void Clear(Sign sign)=>Values.Remove(sign);
        public static bool IsSetOn(Sign sign,uint target)=>Values.TryGetValue(sign,out var value)&&value==target;
    }
}

// Headless movement/ownership doubles. Native rendering, collision meshes and action packets are not exercised.
namespace AnoMech.Scenarios
{
    public interface IScenario { }
    public interface IPhase { }
}
namespace AnoMech.Scenarios.Dsr
{
    public static class DsrZone { public static IPhase P2 => null!; public static IPhase P3 => null!; public static IPhase P4 => null!; public static IPhase P5 => null!; public static IPhase P6 => null!; public static IPhase P7=>null!; }
}
namespace AnoMech.Core.Game.Ai
{
    public interface IScenarioAi { string Name { get; } }
}
namespace AnoMech.Core.Game
{
    public record struct Placement(Vector3 Position, float Rotation);
}
namespace AnoMech.Core.SimObjects
{
    public enum EnemyListMode { OnlyWhenVisible, ScenarioVisible, Never, Manual }
    public record struct EnemySpawnConfig(uint BNpcBaseId, byte Level, EnemyListMode EnemyList, bool IsVisible, Game.Placement Placement, bool DisableLookAt = false, bool WeaponDrawn = false, uint NameId = 0);
    public interface ISimPartyMember { void Knockback(Vector3 source, float distance); void OnKilled(); }
    public class SimCharacter : ISimPartyMember
    {
        public static readonly List<string> Failures = [];
        public static float Time;
        public int Role;
        public Vector3 Position;
        public float Rotation;
        public uint GameObjectId => (uint)Role;
        public bool Active = true;
        private Vector3 destination;
        private float speed;
        private float? facing;
        private bool moving;
        private float knockbackRemaining;
        private readonly Dictionary<ushort, float> statuses = [];
        public bool IsActive => Active;
        public void SetPosition(Vector3 p) { Position = p; }
        public void SetRotation(float r) { Rotation = r; }
        public int MoveCommands;
        public void MoveTo(Vector3 p, float s = 6, float? finalRotation = null)
        {
            MoveCommands++;
            if (knockbackRemaining > 0) return;
            destination = p; speed = s; facing = finalRotation; moving = true;
        }
        public void Knockback(Vector3 source, float distance)
        {
            destination = Position + Vector3.Normalize(Position - source) * distance;
            speed = 25; moving = true; facing = null; knockbackRemaining = distance / speed;
        }
        public void Advance(float dt)
        {
            foreach (var id in statuses.Keys.ToArray()) statuses[id] -= dt;
            if (!moving) return;
            var diff = destination - Position;
            var distance = diff.Length();
            if (distance <= speed * dt) { Position = destination; moving = false; if (facing.HasValue) Rotation = facing.Value; }
            else { Position += Vector3.Normalize(diff) * speed * dt; Rotation = MathF.Atan2(diff.X, diff.Z); }
            knockbackRemaining = Math.Max(0, knockbackRemaining - dt);
        }
        public void Face(Vector3 p) => Rotation = MathF.Atan2(p.X - Position.X, p.Z - Position.Z);
        public void AddStatus(ushort status, float duration, bool playEffects = true) => statuses[status] = duration;
        public void AddStatus(ushort status,float duration,int stacks,bool overrideStacks)=>statuses[status]=duration;
        public void AddStatusParam(ushort status,int param,float duration)=>statuses[status]=duration;
        public void OnKilled(){ Active=false; moving=false; }
        public bool HasStatus(ushort status) => statuses.GetValueOrDefault(status) > 0;
        public void RemoveStatus(ushort status) => statuses.Remove(status);
        public readonly List<(float Time,string Path)> Vfx = [];
        public void AddVfx(string path,float duration=0,bool persistent=true) => Vfx.Add((Time,path));
        public readonly List<uint> LockonVfx = [];
        public readonly List<(uint Id, float Duration)> LockonDurations = [];
        public void AttachLockonVfx(uint id, float duration)
        {
            LockonVfx.Add(id);
            LockonDurations.Add((id, duration));
        }
        public void Despawn() => Active = false;
        public void Die(string cause) => Failures.Add($"{Time:F2}s role {Role}: {cause}");
    }
    public static class DeathExtensions { public static bool IsAlive(this SimCharacter? c) => c?.Active == true; }
    public class SimPartyNpc : SimCharacter { }
    public class SimPlayer : SimCharacter { public bool IsActing {get;set;} }
    public class SimEnemy : SimCharacter
    {
        public int EnmityTank;
        public void SetTankEnmity(SimParty party,int mainTank)=>EnmityTank=mainTank;
        public readonly List<(float Time,ushort Id)> ActionTimelines=[];
        public void PlayActionTimeline(ushort id,ushort loopId=0,ushort baseOverride=0)=>ActionTimelines.Add((Time,id));
        public float Scale = 1;
        public void SetScale(float scale) => Scale = scale;
        public uint BNpcBaseId;
        public readonly List<(ushort Timeline, float Duration)> Entrances = [];
        public readonly List<(float Time, ushort Timeline)> Departures = [];
        public void PlayDeparture(ushort id) => Departures.Add((Time, id));
        public void QueueEntrance(ushort id, float duration) => Entrances.Add((id, duration));
        public uint NameId;
        public EnemyListMode ListMode;
        public bool Visible;
        public bool InEnemyList;
        public void SetVisibleInEnemyList(bool visible) => InEnemyList = visible;
        public readonly List<(float Time, uint Action, float? Duration, float FireDelay)> Casts = [];
        public readonly List<(uint Action,float Delay)> Omens = [];
        public readonly List<(uint Action,uint? Target)> CastTargets = [];
        public readonly List<uint> Dialogue = [];
        public void ShowDialogue(uint id, float duration) => Dialogue.Add(id);
        public void HoldFacing(float? rotation) { if (rotation.HasValue) SetRotation(rotation.Value); }
        public void SetVisible(bool b) => Visible = b;
        public bool WeaponsVisible = true;
        public void SetWeaponsVisible(bool b) => WeaponsVisible = b;
        public bool Targetable;
        public SimCharacter? Target;
        public void SetTarget(SimCharacter? target,bool follow=true)=>Target=target;
        public void SetTargetable(bool b) => Targetable = b;
        public bool Cast(uint action, Vector3? location = null, float? castSeconds = null, uint? targetId = null, float? fireDelay = null, float omenDelay = 0)
        {
            Casts.Add((Time, action, castSeconds, fireDelay ?? 0));
            Omens.Add((action,omenDelay));
            CastTargets.Add((action,targetId));
            return true;
        }
    }
    public class SimParty
    {
        public readonly SimCharacter[] Slots = Enumerable.Range(0, 8).Select(r => (SimCharacter)new SimPartyNpc { Role = r, Position = new(0, 0, 16) }).ToArray();
        public int PlayerRole => Array.FindIndex(Slots,m=>m is SimPlayer) is var index&&index>=0?index:0;
        public SimCharacter? Get(int role) => Slots[role];
        public IEnumerable<SimCharacter> ActiveMembers() => Slots;
        public IEnumerable<(int, SimCharacter)> FilledSlots() => Slots.Select((m, r) => (r, m));
        public void WipeAllPlayers(string cause) => Slots[0].Die(cause);
    }
    public class MapStub { public void AddEffect(uint effect, byte index, uint? resetFlags = null) { } }
    public class SimTether { public void Despawn() { } }
    public class SimWorld
    {
        public readonly List<SimEventObject> EventObjects = [];
        public SimEventObject SpawnEventObject(EventObjectSpawnConfig config) { var obj = new SimEventObject { Config = config }; EventObjects.Add(obj); return obj; }
        public SimTether Tether(SimCharacter? a, SimCharacter? b, ushort id) => new();
        public readonly Game.EventScheduler Events = new();
        public readonly SimParty Party = new();
        public readonly MapStub Map = new();
        public readonly List<SimEnemy> Enemies = [];
        public int PeakEnemies;
        public SimEnemy SpawnEnemy(EnemySpawnConfig config)
        {
            var enemy = new SimEnemy { BNpcBaseId = config.BNpcBaseId, NameId = config.NameId,
                ListMode = config.EnemyList, Visible = config.IsVisible,
                Position = config.Placement.Position, Rotation = config.Placement.Rotation };
            Enemies.Add(enemy);
            PeakEnemies = Math.Max(PeakEnemies, Enemies.Count(e => e.Active));
            return enemy;
        }
        public void SpawnOmen(string path, Game.Placement p, Vector3 scale, float duration) { }
        public void SpawnGroundEffect(string path, Game.Placement p, float duration, float scale = 1) { }
    }
}

namespace AnoMech.Core.SimObjects
{
    public class EventObjectSpawnConfig { public uint EObjId {get;init;} public Game.Placement Placement {get;init;} public float Lifetime {get;init;} }
    public class SimEventObject { public EventObjectSpawnConfig Config = new(); public bool Active = true; public void Despawn() => Active = false; }
}

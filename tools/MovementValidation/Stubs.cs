using System.Numerics;

namespace AnoMech.Core.Game
{
    public record struct Placement(Vector3 Position, float Rotation)
    {
        public Placement Face(Vector3 target) => this with { Rotation = MathF.Atan2(target.X - Position.X, target.Z - Position.Z) };
        public Placement MoveForward(float distance) => this with { Position = Position + new Vector3(MathF.Sin(Rotation), 0, MathF.Cos(Rotation)) * distance };
        public float DistanceSq(SimObjects.SimCharacter other) => Vector3.DistanceSquared(Position, other.Position);
    }
    public sealed class Obstacles
    {
        public Vector2 NearestClearOnSegment(Vector2 start, Vector2 end, float t, float min, float max) => Vector2.Lerp(start, end, Math.Clamp(t, min, max));
        public Vector2 ClampOutside(Vector2 position) => position;
        public Vector2 Steer(Vector2 position, Vector2 desired, float distance) => desired;
    }
}
namespace AnoMech.Core.SimObjects
{
    public sealed class SimCharacter
    {
        public bool AnimationLock;
        public Vector3 Position;
        public float Rotation;
        public float HitboxRadius => 1;
        public Game.Obstacles Obstacles { get; } = new();
        public Game.Placement Placement() => new(Position, Rotation);
        public void SetPosition(Game.Placement value) { Position = value.Position; Rotation = value.Rotation; }
        public void SetRotation(float rotation) => Rotation = rotation;
        public void PlayActionTimeline(ushort timeline, ushort baseOverride) { }
        public void ResetActionTimeline() { }
    }
    public static class CharacterExtensions
    {
        public static bool IsAlive(this SimCharacter? character) => character != null;
    }
    public sealed class SimTether
    {
        public SimCharacter? A;
        public SimCharacter? B;
        public bool IsActive;
    }
}

using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

var actor = new SimCharacter();
var movement = new Movement(actor);
movement.MoveTo(new(10, 0, 0), 6);
movement.Tick(0);
movement.Tick(-.1f);
if (actor.Position != Vector3.Zero) throw new Exception("Zero/negative delta teleported the NPC");
movement.MoveTo(new(10, 0, 0), 0);
movement.Tick(1);
if (actor.Position != Vector3.Zero) throw new Exception("Zero speed teleported the NPC");
movement.MoveTo(new(10, 0, 0), 6);
movement.Tick(.1f);
if (Vector3.Distance(actor.Position, new(.6f, 0, 0)) > .0001f)
    throw new Exception("Normal movement must advance at the requested speed");
actor.AnimationLock = true;
var before = actor.Position;
movement.Tick(1);
if (actor.Position != before) throw new Exception("Animation-locked NPC moved");
actor.AnimationLock = false;
for (var frame = 0; frame < 120; frame++)
{
    before = actor.Position;
    movement.MoveTo(new(10, 0, 0), 6);
    movement.Tick(1f / 60);
    if (Vector3.Distance(before, actor.Position) > .1001f)
        throw new Exception("Repeated movement commands exceeded the speed limit");
}
if (actor.Position != new Vector3(10, 0, 0)) throw new Exception("NPC did not reach destination");
var player = new SimCharacter();
var playerMovement = new PlayerMovement(player);
playerMovement.MoveTo(new(10, 0, 0));
playerMovement.Tick(1);
if (player.Position != Vector3.Zero) throw new Exception("AI moved the player");
Console.WriteLine("Production Movement: zero/negative delta, zero speed, speed bounds, repeated commands, animation lock and player ownership passed.");

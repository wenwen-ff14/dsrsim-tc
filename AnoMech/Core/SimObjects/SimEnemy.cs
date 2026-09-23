using AnoMech.Core.Game;
using AnoMech.Helpers;
using AnoMech.Pointers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Numerics;

namespace AnoMech.Core.SimObjects;

// Placement.Position is scenario-local (offset from SimWorld.ScenarioOrigin), same
// coordinate space as the rest of the SimXxx API: +X = east, +Z = south.
// Placement.Rotation is absolute radians: 0 = south, π/2 = east, π = north, -π/2 = west.
// ModelCharaId (non-zero) overrides the BNpcBase visual, e.g. a no-shield variant.
// Hitbox radius = BNpcBase.Scale × ModelChara's unscaled radius, unless HitboxRadius
// (non-zero) overrides it — decoupling the clickable/targetable hitbox from Scale.

// Whether a SimEnemy shows in the _EnemyList HUD (read each frame by EnmityHud.Refresh).
// Always          — listed while alive.
// OnlyWhenVisible — follows the engine's DrawObject.IsVisible; for adds that warp
//                   in/out. Don't combine with SetModelState (its rebuild briefly
//                   DisableDraws and flaps the list); transforming bosses use Always.
// ScenarioVisible — follows SetVisible immediately, independently of model loading.
// Never           — never listed (AOE-source dummies, tether endpoints).
// Manual          — scenario drives it via SetInEnemyList(bool); default false.
public enum EnemyListMode
{
    Always,
    OnlyWhenVisible,
    ScenarioVisible,
    Never,
    Manual,
}

public record struct EnemySpawnConfig(
    uint BNpcBaseId,
    uint NameId = 0,
    byte Level = 0,
    bool Targetable = false,
    EnemyListMode EnemyList = EnemyListMode.Always,
    bool IsVisible = true,
    Placement Placement = default,
    uint ModelCharaId = 0,
    float Scale = 0f,    // 0 = use BNpcBase.Scale
    float HitboxRadius = 0f,    // 0 = ModelChara unscaled radius × Scale
    byte? InitialModeAttributeFlags = null,
    bool DisableLookAt = false,
    bool WeaponDrawn = false);

public sealed unsafe class SimEnemy : SimNpc
{
    // Cast bar, action-effect release, omen telegraph, and animation lock live in
    // SimCast. SimEnemy just converts target coords to world space and reads IsBusy.
    private readonly SimCast cast;

    // Model and weapon loads finish asynchronously; reapply the scenario's visibility
    // after native casts and weapon initialization, including on the first draw.
    private bool desiredVisible = true;
    private bool currentVisible = true;
    private bool visibilityDirty = true;
    private nint visibilityDrawObject;
    private float? heldFacing;
    private bool disableLookAt;
    private bool weaponDrawn;
    private bool weaponsInitialized;
    private bool hiddenWeaponsRemoved;
    private bool departurePlaying;
    private bool battleIdleActive;
    private bool battleStanceInitialized;
    private nint poseDrawObject;
    private float poseRetryRemaining;
    private ushort entranceTimeline;
    private float entranceDuration;
    private float entranceRemaining;
    private bool entrancePlaying;
    private float entranceRevealRemaining;
    private bool weaponsVisible = true;
    private bool? appliedWeaponVisibility;
    private readonly nint[] weaponDrawObjects = new nint[3];

    public void QueueEntrance(ushort timelineId, float duration)
    {
        entranceTimeline = timelineId;
        entranceDuration = duration;
        ReconcileVisibility();
    }

    public void PlayDeparture(ushort timelineId)
    {
        StopEntrance();
        departurePlaying = true;
        battleIdleActive = false;
        PlayActionTimeline(timelineId);
    }

    private void StopEntrance()
    {
        entranceTimeline = 0;
        entranceRevealRemaining = 0;
        if (!entrancePlaying) return;
        entrancePlaying = false;
        var chara = BattleCharaPtr;
        if (chara != null && chara->Timeline.TimelineSequencer.Parent != null)
            chara->Timeline.TimelineSequencer.SetSlotTimeline(0, 0);
        battleIdleActive = false;
    }

    public uint BNpcBaseId { get; }

    public void SetScale(float scale)
    {
        var obj = BattleCharaPtr;
        if (obj == null || !float.IsFinite(scale) || scale <= 0) return;
        obj->Scale = scale;
        obj->HitboxRadius = obj->ModelContainer.UnscaledRadius * scale;
        if (obj->DrawObject != null) obj->DrawObject->Object.Scale = new(scale);
    }


    // Live-read via GameObject::GetName() (vfunc 6, resolves NameId -> BNpcName) —
    // same path the target bar uses, so engine-driven renames mid-fight propagate
    // (e.g. TOP P5 Sigma Omega: 1DD3 -> 1DD4 -> 1E0F -> 2FE2). Reading the
    // GameObject.Name[] buffer directly does NOT work for doppels — the engine never
    // refreshes it on rename. Falls back to the spawn-time name mid-despawn.
    private readonly string displayName;
    public string DisplayName
    {
        get
        {
            var chara = BattleCharaPtr;
            if (chara == null) return displayName;
            var name = Plugin.DataManager.GetExcelSheet<BNpcName>().TryGetRow(chara->NameId, out var row)
                ? row.Singular.ExtractText() : ((GameObject*)chara)->GetName().ToString();
            return string.IsNullOrEmpty(name) ? displayName : name;
        }
    }

    public EnemyListMode EnemyListMode { get; }
    private bool manualInEnemyList;

    // OnlyWhenVisible reads the live DrawObject.IsVisible flag, so any draw-lifecycle
    // toggle is reflected without extra plumbing; Manual lets the scenario drive it.
    public bool InEnemyList => EnemyListMode switch
    {
        EnemyListMode.Always          => true,
        EnemyListMode.Never           => false,
        EnemyListMode.Manual          => manualInEnemyList,
        EnemyListMode.OnlyWhenVisible => IsEngineVisible(),
        EnemyListMode.ScenarioVisible => desiredVisible,
        _ => false,
    };

    public bool IsCasting => cast.IsCasting;

    public string DescribePose()
    {
        var chara = BattleCharaPtr;
        if (chara == null) return $"{displayName}: inactive";
        var result = new System.Text.StringBuilder();
        result.AppendLine($"{DisplayName}: BNpc={BNpcBaseId:X}, Model={chara->ModelContainer.ModelCharaId}, Skeleton={chara->ModelContainer.ModelSkeletonId}");
        result.AppendLine($"Ready={chara->IsReadyToDraw()}, ClassJob={chara->ClassJob}, Flags3={chara->Timeline.Flags3:X}, BaseOverride={chara->Timeline.BaseOverride}, ModelState={chara->Timeline.ModelState}, AnimationState={chara->Timeline.AnimationState[0]}/{chara->Timeline.AnimationState[1]}");
        result.AppendLine($"WeaponsInitialized={weaponsInitialized}, StanceInitialized={battleStanceInitialized}, Idle={battleIdleActive}, Cast={CastActionId}");
        result.AppendLine("Slots=" + string.Join(",", chara->Timeline.TimelineSequencer.TimelineIds.ToArray()));
        for (var slot = 0; slot < 3; slot++)
            result.AppendLine($"Weapon[{slot}]={chara->DrawData.WeaponData[slot].ModelId.Value:X}, Draw={(nint)DrawDataPointers.WeaponDrawObject(&chara->DrawData, slot):X}");
        if (chara->DrawObject != null && chara->IsReadyToDraw())
        {
            var model = (FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CharacterBase*)chara->DrawObject;
            result.AppendLine($"AnimationVariant={model->AnimationVariant}");
            for (var i = 0; i < model->SkeletonAnimationContainers.Length; i++)
            {
                ref var container = ref model->SkeletonAnimationContainers[i];
                AppendPacks($"PAP[{i},1]", container.PapVector1);
                AppendPacks($"PAP[{i},2]", container.PapVector2);
                AppendPacks($"PAP[{i},3]", container.PapVector3);
            }
        }
        return result.ToString();

        void AppendPacks(string label, FFXIVClientStructs.STD.StdVector<FFXIVClientStructs.Interop.Pointer<FFXIVClientStructs.FFXIV.Client.System.Resource.Handle.ResourceHandle>> packs)
        {
            if (packs.LongCount < 0 || packs.LongCount > 64)
            {
                result.AppendLine($"{label}: invalid count {packs.LongCount}");
                return;
            }
            foreach (var resource in packs)
                if (resource.Value != null) result.AppendLine($"{label}: {resource.Value->FileName}");
        }
    }
    public uint CastActionId => cast.ActionId;
    public float CastProgress => cast.Progress;

    internal SimEnemy(int index, uint bNpcBaseId, string displayName, EnemyListMode enemyListMode, Coordinates coordinates) : base(index, coordinates)
    {
        BNpcBaseId = bNpcBaseId;
        this.displayName = displayName;
        EnemyListMode = enemyListMode;
        cast = new SimCast(this, coordinates);
    }

    // Allocates a BattleChara, configures it as a BattleNpc per the supplied
    // config, and returns a SimEnemy wrapping it. Caller is responsible for
    // registering the result in the world's children list (so reset/teardown
    // covers it). Returns null on missing LocalPlayer, BNpcBase miss, or
    // CreateBattleChara failure.
    internal static SimEnemy? Spawn(EnemySpawnConfig config, SimWorld world)
    {
        var player = Plugin.ObjectTable.LocalPlayer;
        if (player == null) return null;

        var bnpcSheet = Plugin.DataManager.GetExcelSheet<BNpcBase>();
        if (!bnpcSheet.TryGetRow(config.BNpcBaseId, out var bnpc))
        {
            Plugin.Log.Warning($"BNpcBase row {config.BNpcBaseId} (0x{config.BNpcBaseId:X}) not found");
            return null;
        }

        var modelCharaId = config.ModelCharaId != 0 ? config.ModelCharaId : bnpc.ModelChara.RowId;
        var modelCharaSheet = Plugin.DataManager.GetExcelSheet<ModelChara>();
        if (!modelCharaSheet.TryGetRow(modelCharaId, out var modelChara))
        {
            Plugin.Log.Warning($"ModelChara row {config.BNpcBaseId} (0x{config.BNpcBaseId:X}) not found");
            return null;
        }

        if (!CharacterManagerHelper.CreateCharacter(out var idx, out var obj)) return null;

        var gameObj = (GameObject*)obj;
        var chara = (BattleChara*)obj;
        // Engine's canonical BNpc initializer — populates ModelContainer from BNpcBase,
        // including ModeAttributeFlags (body sub-mesh, e.g. Omega-M's shield). Must run
        // before our overrides below.
        chara->CharacterSetup.SetupBNpc(config.BNpcBaseId, config.NameId);
        chara->ObjectKind = ObjectKind.BattleNpc;
        chara->Position = world.Coordinates.ToGlobal(config.Placement.Position);
        chara->SetRotation(MathUtil.NormalizeRotation(config.Placement.Rotation));
        var scale = config.Scale > 0f ? config.Scale : bnpc.Scale;
        chara->Scale = scale;
        chara->ModelContainer.ModelCharaId = (int)modelCharaId;
        chara->SEPack = bnpc.SEPack;

        var nativeHitbox = true;

        // From Client::Game::Character::CharacterSetupContainer_SetupRaw
        switch (modelChara.Type)
        {
            case 1:
                // TODO: This Type in the game's .exe is a bit complex, for now we just fallback to the previous solving method
                var hitboxRadius = config.HitboxRadius > 0f ? config.HitboxRadius : ResolveHitboxRadius(modelCharaId, scale);
                chara->HitboxRadius = hitboxRadius;
                nativeHitbox = false;
                break;
            case 2:
                chara->ModelContainer.ModelSkeletonId = modelChara.Model + 10000;
                break;
            case 3:
                chara->ModelContainer.ModelSkeletonId = modelChara.Model;
                break;
        }

        if (nativeHitbox)
        {
            // SetupBNpc already resolves the native radius; overrides use the selected model's row.
            if (config.ModelCharaId != 0 || chara->ModelContainer.UnscaledRadius <= 0f)
                chara->ModelContainer.UnscaledRadius = ResolveHitboxRadius(modelCharaId, 1f);
            chara->HitboxRadius = config.HitboxRadius > 0f
                ? config.HitboxRadius
                : chara->Scale * chara->ModelContainer.UnscaledRadius;
        }

        // Engine-resolved name (vfunc 6), same source as the nameplate, so the Name[]
        // buffer we stamp below stays consistent with the rest of the UI.
        var displayName = config.NameId != 0 && Plugin.DataManager.GetExcelSheet<BNpcName>().TryGetRow(config.NameId, out var nameRow)
            ? nameRow.Singular.ExtractText() : gameObj->GetName().ToString();
        if (string.IsNullOrEmpty(displayName)) displayName = $"BNpc {config.BNpcBaseId:X}";
        GameObjectHelper.WriteName(gameObj, displayName);
        obj->RenderFlags = 0;

        chara->CharacterSetup.CopyFromCharacter((Character*)chara, CharacterSetupContainer.CopyFlags.None);

        chara->BattleNpcSubKind = BattleNpcSubKind.Combatant;
        chara->MaxHealth = 1_000_000;
        chara->Health = 1_000_000;
        chara->Battalion = 4;
        chara->Character.CharacterData.Flags |= 0x03;
        chara->CombatTagType = 1;
        chara->CombatTaggerId = ((GameObject*)player.Address)->GetGameObjectId();
        chara->Mode = CharacterModes.Normal;
        chara->ModeParam = 0;
        if (config.InitialModeAttributeFlags is { } maf)
            ModelContainerPointers.ModeAttributeFlags(&chara->ModelContainer) = maf;
        chara->CastInfo.IsCasting = false;
        if (config.NameId != 0) chara->NameId = config.NameId;
        if (config.Level != 0) chara->Level = config.Level;

        Plugin.Log.Info($"SimEnemy: spawned BNpcBase {config.BNpcBaseId} (ModelChara {bnpc.ModelChara.RowId}, scale {bnpc.Scale}) at index {idx}");
        var enemy = new SimEnemy(idx, config.BNpcBaseId, displayName, config.EnemyList, world.Coordinates)
        {
            disableLookAt = config.DisableLookAt,
            weaponDrawn = config.WeaponDrawn,
        };
        // Mirror the native position/rotation writes above into the C#-side fields.
        enemy.SetPosition(config.Placement);
        enemy.SetTargetable(config.Targetable);
        if (!config.IsVisible) enemy.SetVisible(false);
        return enemy;
    }

    private static float ResolveHitboxRadius(uint modelCharaId, float scale)
    {
        const float DefaultUnscaledRadius = 0.5f;
        var sheet = Plugin.DataManager.GetExcelSheet<ModelChara>();
        var unscaled = DefaultUnscaledRadius;
        if (sheet.TryGetRow(modelCharaId, out var row) && row.Unknown0 > 0f)
            unscaled = row.Unknown0;
        return unscaled * scale;
    }

    public override void Despawn()
    {
        SetVisible(false);
        Movement.Follow(null);
        cast.Despawn();
        base.Despawn();
    }

    /// <summary>
    /// Sets the targetable status of this <see cref="SimEnemy"/>, which will reflect in their Nameplate and in the Enemy List (if visible there).
    /// </summary>
    /// <param name="targetable">
    /// If <see langword="true"/>, then the Nameplate will be visible, and able to target them using the Enemy List.
    /// If <see langword="false"/>, then the Nameplate will not be visible, and not able to target them using the Enemy List.
    /// </param>
    public void SetTargetable(bool targetable)
    {
        var chara = BattleCharaPtr;
        if (chara == null) return;
        if (targetable)
        {
            chara->TargetableStatus |= (ObjectTargetableFlags)1 | ObjectTargetableFlags.IsTargetable;
        }
        else
        {
            chara->TargetableStatus &= ~((ObjectTargetableFlags)1 | ObjectTargetableFlags.IsTargetable);
        }
    }

    /// <summary>
    /// Only executed when <see cref="EnemyListMode"/> is <see cref="EnemyListMode.Manual"/>
    /// </summary>
    /// <param name="inEnemyList">Will make the Enemy appear or not in the Enemy List (Enmity List)</param>
    public void SetVisibleInEnemyList(bool inEnemyList)
    {
        if (EnemyListMode != EnemyListMode.Manual)
        {
            Plugin.Log.Warning($"SetInEnemyList({inEnemyList}) ignored: SimEnemy {DisplayName} has mode {EnemyListMode}; declare EnemyListMode.Manual in EnemySpawnConfig to use explicit toggles.");
            return;
        }
        manualInEnemyList = inEnemyList;
    }

    /// <summary>
    /// Sets the target of this <see cref="SimEnemy"/>.
    /// </summary>
    /// <remarks>For now, this is purely visual and does not contain any logic relating to auto-attacks or similar.</remarks>
    /// <param name="target">The <see cref="SimCharacter.GameObjectId"/> will be retrieved and used as the TargetId. If <see langword="null"/>, then the target is cleared.</param>
    /// <param name="follow">If <paramref name="target"/> is valid, this will determine if the <see cref="SimEnemy"/> should now follow <paramref name="target"/> or not.</param>
    /// <param name="speed">If <paramref name="target"/> is valid and <paramref name="follow"/> is <see langword="true"/>, this will be the speed that the <see cref="SimEnemy"/> will follow the <paramref name="target"/></param>
    public void SetTarget(SimCharacter? target, bool follow = true, float speed = 6f)
    {
        if (target == null)
        {
            BattleCharaPtr->TargetId = 0xE0000000;
        }
        else
        {
            BattleCharaPtr->TargetId = target.GameObjectId;

            if (follow)
            {
                Follow(target);
            }
        }
    }

    public void SetVisible(bool visible)
    {
        desiredVisible = visible;
        visibilityDirty = true;
        if (visible)
        {
            departurePlaying = false;
            hiddenWeaponsRemoved = false;
        }
        ReconcileVisibility();
        if (!visible) RemoveHiddenWeapons();
    }

    private void RemoveHiddenWeapons()
    {
        var chara = BattleCharaPtr;
        if (!weaponDrawn || hiddenWeaponsRemoved || chara == null || !chara->IsReadyToDraw()) return;
        // Weapon scene objects have their own visibility and may be re-enabled by native timelines.
        // Let LoadWeapon retire them; changing only DrawObject.IsVisible leaves the attachments alive.
        for (var slot = 0; slot < 3; slot++)
            DrawDataPointers.LoadWeapon(&chara->DrawData, (DrawDataContainer.WeaponSlot)slot, default);
        hiddenWeaponsRemoved = true;
        weaponsInitialized = battleStanceInitialized = battleIdleActive = false;
        appliedWeaponVisibility = null;
        System.Array.Clear(weaponDrawObjects);
    }

    public void SetWeaponsVisible(bool visible)
    {
        weaponsVisible = visible;
        ReconcileVisibility();
    }

    // Reapply after asynchronous model creation and native idle/look-at updates.
    public void HoldFacing(float? rotation)
    {
        heldFacing = rotation;
        if (rotation is { } value) SetRotation(value);
    }

    public void Follow(SimCharacter? target = null, float speed = 6f) => Movement.Follow(target, speed);

    private void ReconcileVisibility()
    {
        var obj = BattleCharaPtr;
        if (obj == null)
        {
            return;
        }

        var visible = desiredVisible && entranceTimeline == 0 && entranceRevealRemaining <= 0;
        var draw = obj->DrawObject;
        if (draw != null)
        {
            // Native action timelines may hide the body during a teleport.
            // Only an explicit reveal, entrance transition or new model may show it again.
            if (!visible || visibilityDirty || currentVisible != visible || visibilityDrawObject != (nint)draw)
                draw->IsVisible = visible;
            visibilityDirty = false;
        }
        visibilityDrawObject = (nint)draw;
        if (weaponDrawn)
        {
            var showWeapons = visible && weaponsVisible && obj->DrawObject != null && obj->DrawObject->IsVisible;
            var visibilityChanged = appliedWeaponVisibility != showWeapons;
            if (visibilityChanged) obj->DrawData.HideWeapons(!showWeapons);
            appliedWeaponVisibility = showWeapons;
            for (var slot = 0; slot < 3; slot++)
            {
                var weapon = DrawDataPointers.WeaponDrawObject(&obj->DrawData, slot);
                if (weapon != null && (!showWeapons || visibilityChanged || weaponDrawObjects[slot] != (nint)weapon))
                    weapon->IsVisible = showWeapons;
                weaponDrawObjects[slot] = (nint)weapon;
            }
        }
        if (currentVisible == visible) return;
        currentVisible = visible;

        Plugin.Log.Debug($"[SimEnemy.ReconcileVisibility] {DisplayName}'s visibility was set to {visible}");
    }

    // Authoritative draw state (DrawObject.Flags bits 0 and 3, set by Enable/DisableDraw).
    // False during the async model-load window where DrawObject is still null.
    private bool IsEngineVisible()
    {
        var obj = BattleCharaPtr;
        if (obj == null) return false;
        var draw = obj->DrawObject;
        return draw != null && draw->IsVisible;
    }

    // Engine doesn't expose post-action animation-lock duration via EXD — the
    // real value only ships in the server's ActionEffect packet. 0.6s is a
    // reasonable approximation for most boss abilities; if a scenario needs
    // tighter timing we can derive per-action values from captured ACT logs.
    public bool Cast(uint actionId, Vector3? targetLocation = null, float? castSeconds = null, GameObjectId? targetId = null, float omenDelay = 0f, float omenRotate = 0f, byte animationVariation = 0, float animationLock = 0.6f, float? fireDelay = null)
    {
        StopEntrance();
        if (weaponDrawn && BattleCharaPtr != null)
        {
            BattleCharaPtr->Timeline.BaseOverride = 0;
            battleIdleActive = false;
        }
        // targetLocation stays scenario-local; SimCast lifts to world at native boundaries.
        return cast.Start(actionId, targetLocation, castSeconds, targetId, omenDelay, omenRotate, animationVariation, animationLock, fireDelay);
    }

    public void NativeCast(uint actionId, ActionType actionType, float omenDelay, float castTime, bool interruptible, float? rotation = null, Vector3? position = null, GameObjectId? targetId = null, GameObjectId? ballistaId = null)
    {
        cast.NativeCast(actionId, actionType, omenDelay, castTime, interruptible, rotation, position, targetId, ballistaId);
    }

    public void NativeActionEffect(uint actionId, float animationLock, ushort spellId, byte animationVariaton, ActionType actionType, byte flags, float? rotation = null, Vector3? position = null, GameObjectId? animationTargetId = null, GameObjectId? actionTargetId = null, GameObjectId? ballistaId = null)
    {
        cast.NativeActionEffect(actionId, animationLock, spellId, animationVariaton, actionType, flags, rotation, position, animationTargetId, actionTargetId, ballistaId);
    }

    public override bool AnimationLock => cast.IsBusy;

    public override void Tick(float deltaSeconds)
    {
        entranceRevealRemaining = System.Math.Max(0, entranceRevealRemaining - deltaSeconds);
        if (entrancePlaying)
        {
            entranceRemaining -= deltaSeconds;
            if (entranceRemaining <= 0 || Movement.IsMoving) StopEntrance();
        }
        base.Tick(deltaSeconds);
        if (heldFacing is { } rotation) SetRotation(rotation);
        cast.Tick(deltaSeconds);
        poseRetryRemaining = System.Math.Max(0, poseRetryRemaining - deltaSeconds);
        if (weaponDrawn) ReconcileBattlePose();
        else if (desiredVisible && entranceTimeline != 0 && BattleCharaPtr != null &&
                 BattleCharaPtr->IsReadyToDraw() && BattleCharaPtr->Timeline.TimelineSequencer.Parent != null)
        {
            PlayActionTimeline(entranceTimeline);
            entranceTimeline = 0;
            entranceRemaining = entranceDuration;
            entrancePlaying = true;
            entranceRevealRemaining = 1f / 30f;
        }
        ReconcileVisibility();
        if (disableLookAt) ClearLookAt();
    }

    private void ReconcileBattlePose()
    {
        var chara = BattleCharaPtr;
        if (chara == null || chara->DrawObject == null || !chara->IsReadyToDraw() ||
            chara->Timeline.TimelineSequencer.Parent == null) return;

        if (poseDrawObject != (nint)chara->DrawObject)
        {
            poseDrawObject = (nint)chara->DrawObject;
            weaponsInitialized = battleIdleActive = battleStanceInitialized = false;
            hiddenWeaponsRemoved = false;
            poseRetryRemaining = 0;
        }

        if (!desiredVisible)
        {
            RemoveHiddenWeapons();
            return;
        }
        if (departurePlaying) return;

        if (!weaponsInitialized)
        {
            if (Plugin.DataManager.GetExcelSheet<BNpcBase>().TryGetRow(BNpcBaseId, out var npc) &&
                npc.NpcEquip.ValueNullable is { } equipment)
            {
                var mainHand = new WeaponModelId { Value = equipment.ModelMainHand,
                    Stain0 = (byte)equipment.DyeMainHand.RowId, Stain1 = (byte)equipment.Dye2MainHand.RowId };
                var offHand = new WeaponModelId { Value = equipment.ModelOffHand,
                    Stain0 = (byte)equipment.DyeOffHand.RowId, Stain1 = (byte)equipment.Dye2OffHand.RowId };
                DrawDataPointers.LoadWeapon(&chara->DrawData, DrawDataContainer.WeaponSlot.MainHand, mainHand);
                DrawDataPointers.LoadWeapon(&chara->DrawData, DrawDataContainer.WeaponSlot.OffHand, offHand);
                appliedWeaponVisibility = null;
                ReconcileVisibility();
                Plugin.Log.Debug($"SimEnemy {BNpcBaseId:X}: restored weapons {mainHand.Value:X}/{offHand.Value:X}");
            }
            weaponsInitialized = true;
            poseRetryRemaining = .25f;
            return;
        }

        const ushort BattleIdle = 34;
        if (entrancePlaying) return;
        if ((chara->Timeline.Flags3 & 0x40) == 0) battleStanceInitialized = false;
        if (Movement.IsMoving || cast.IsBusy || !desiredVisible ||
            (entranceTimeline == 0 && !IsEngineVisible()))
        {
            if (chara->Timeline.BaseOverride == BattleIdle) chara->Timeline.BaseOverride = 0;
            battleIdleActive = false;
        }
        else if (poseRetryRemaining <= 0 && !battleStanceInitialized)
        {
            chara->Timeline.BaseOverride = 0;
            battleStanceInitialized = TimelinePointers.DrawWeapon(&chara->Timeline);
            battleIdleActive = false;
            poseRetryRemaining = 1f;
        }
        else if (poseRetryRemaining <= 0 && entranceTimeline != 0)
        {
            PlayActionTimeline(entranceTimeline);
            entranceTimeline = 0;
            entranceRemaining = entranceDuration;
            entrancePlaying = true;
            // Give the native animation update a frame to apply the airborne pose.
            entranceRevealRemaining = 1f / 30f;
            battleIdleActive = false;
        }
        else if (poseRetryRemaining <= 0 && !battleIdleActive)
        {
            // The server animation lock can end before the body/upper-body action.
            // Do not replace a still-selected native skill with forced idle.
            var slots = chara->Timeline.TimelineSequencer.TimelineIds;
            if (slots[0] is not (0 or 1 or BattleIdle) || slots[1] != 0) return;
            PlayActionTimeline(BattleIdle, loopId: BattleIdle, baseOverride: BattleIdle);
            battleIdleActive = true;
        }
        else if (poseRetryRemaining <= 0 && battleIdleActive)
            chara->Timeline.BaseOverride = BattleIdle;
    }

    private void ClearLookAt()
    {
        var chara = BattleCharaPtr;
        if (chara == null) return;
        chara->TargetId = 0xE0000000;
        chara->SoftTargetId = 0xE0000000;
        chara->LookAt.IsFacingCamera = false;
        chara->LookAt.BannerCameraFollowFlag = LookAtContainer.BannerCameraFollowFlags.None;
        // Look targets are independent of TargetId and can twist the torso, head and weapon pose.
        foreach (ref var param in chara->LookAt.Controller.Params)
        {
            param.TargetParam.Type = CharacterLookAtTargetParam.TargetInfoType.None;
            param.TargetParam.TargetId = default;
        }
    }

    public CharacterFind<T> Find<T>(List<T> targets) where T : IPositioned
    {
        return new CharacterFind<T>(targets);
    }
}

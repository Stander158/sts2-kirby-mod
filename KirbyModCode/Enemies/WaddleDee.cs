using System.Reflection;
using Godot;
using HarmonyLib;
using KirbyMod.Characters;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace KirbyMod.Enemies;

/// <summary>
/// 瓦豆鲁迪 (Waddle Dee): weak Overgrowth enemy, 4-6 HP (6-8 on Tough Enemies). Alternates 撞击 (Tackle, 8 damage, 10 on
/// Deadly Enemies) and 溜达 (Wander, does nothing, no intent icon). Has No Ability: inhaling it never gives an ability.
/// A static sprite: the attack is a lunge towards its target, hits shake it, and it fades out when it dies.
/// </summary>
[RegisterMonster]
public sealed class WaddleDee : ModMonsterTemplate
{
    private const string VisualsScene = KirbyTheme.Scenes + "waddle_dee_visuals.tscn";
    private const float LungeDistance = 70f;

    private bool _startsWithWander;

    public override MonsterAssetProfile AssetProfile => new(VisualsScene);

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 6, 4);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 8, 6);

    private static int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);

    // No sound bank of its own: the default death sound path does not exist.
    public override bool HasDeathSfx => false;

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

    /// <summary>Set by the encounter before combat, so the Dees' intents alternate.</summary>
    public bool StartsWithWander
    {
        get => _startsWithWander;
        set
        {
            AssertMutable();
            _startsWithWander = value;
        }
    }

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(VisualsScene, PackedScene.GenEditState.Disabled);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var tackle = new MoveState("TACKLE_MOVE", Tackle, new SingleAttackIntent(TackleDamage));
        var wander = new MoveState("WANDER_MOVE", _ => Task.CompletedTask, new HiddenIntent());
        tackle.FollowUpState = wander;
        wander.FollowUpState = tackle;
        return new MonsterMoveStateMachine([tackle, wander], StartsWithWander ? wander : tackle);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<NoAbilityPower>(new ThrowingPlayerChoiceContext(), Creature, 1, null, null);
    }

    private async Task Tackle(IReadOnlyList<Creature> targets)
    {
        Lunge(targets.FirstOrDefault());
        await DamageCmd.Attack(TackleDamage).FromMonster(this).WithAttackerAnim("Attack", 0.15f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    /// <summary>Jumps at <paramref name="target" /> and back (the sprite has no attack animation).</summary>
    private void Lunge(Creature? target)
    {
        var room = NCombatRoom.Instance;
        if (room?.GetCreatureNode(Creature) is not { } self || self.Body is not { } body)
            return;

        var targetNode = room.GetCreatureNode(target);
        var side = targetNode != null && targetNode.GlobalPosition.X > self.GlobalPosition.X ? 1f : -1f;
        var home = body.Position;
        var tween = body.CreateTween();
        tween.TweenProperty(body, "position", home + Vector2.Right * side * LungeDistance, 0.12)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(body, "position", home, 0.2).SetEase(Tween.EaseType.InOut);
    }
}

/// <summary>4 Waddle Dees, alternating Tackle and Wander. Weak, Overgrowth only, and only in runs with a Kirby.</summary>
[RegisterActEncounter(typeof(Overgrowth))]
public sealed class WaddleDeesWeak : ModEncounterTemplate
{
    private const int Count = 4;

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<WaddleDee>()];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        Enumerable.Range(0, Count).Select(i =>
        {
            var dee = (WaddleDee)ModelDb.Monster<WaddleDee>().ToMutable();
            dee.StartsWithWander = i % 2 == 1;
            return ((MonsterModel)dee, (string?)null);
        }).ToList();
}

/// <summary>
/// The act's weak encounters are rolled once, at run start: without a Kirby in the run, Waddle Dees are left out of the
/// list for that roll. The act caches the list (and run acts are shallow copies of the canonical one, cache included),
/// so the filtered list is set on this act only for the roll and the cache is cleared afterwards.
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
public static class WaddleDeeKirbyOnlyPatch
{
    private static readonly MethodInfo RunStateGetter = AccessTools.PropertyGetter(typeof(RunManager), "State");

    [HarmonyPrefix]
    public static void Prefix(ActModel __instance, ref IEnumerable<EncounterModel>? ____allWeakEncounters)
    {
        if (RunStateGetter.Invoke(RunManager.Instance, null) is not RunState state
            || state.Players.Any(player => player.Character is Kirby))
            return;

        ____allWeakEncounters = __instance.AllWeakEncounters.Where(e => e is not WaddleDeesWeak).ToList();
    }

    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EncounterModel>? ____allWeakEncounters) =>
        ____allWeakEncounters = null;
}

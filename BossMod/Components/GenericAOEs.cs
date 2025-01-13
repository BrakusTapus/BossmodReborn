﻿namespace BossMod.Components;

// generic component that shows arbitrary shapes representing avoidable aoes
public abstract class GenericAOEs(BossModule module, ActionID aid = default, string warningText = "GTFO from aoe!") : CastCounter(module, aid)
{
    public record struct AOEInstance(AOEShape Shape, WPos Origin, Angle Rotation = default, DateTime Activation = default, uint Color = 0, bool Risky = true, ulong? ActorID = null)
    {
        public readonly bool Check(WPos pos) => Shape.Check(pos, Origin, Rotation);
    }

    public string WarningText = warningText;

    public abstract IEnumerable<AOEInstance> ActiveAOEs(int slot, Actor actor);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var aoe in ActiveAOEs(slot, actor))
        {
            if (aoe.Risky && aoe.Check(actor.Position))
            {
                hints.Add(WarningText);
                break;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var c in ActiveAOEs(slot, actor))
            if (c.Risky)
                hints.AddForbiddenZone(c.Shape, c.Origin, c.Rotation, c.Activation);
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var c in ActiveAOEs(pcSlot, pc))
            c.Shape.Draw(Arena, c.Origin, c.Rotation, c.Color == 0 ? Colors.AOE : c.Color);
    }
}

// For simple AOEs, formerly known as SelfTargetedAOEs andLocationTargetedAOEs, that happens at the end of the cast
public class SimpleAOEs(BossModule module, ActionID aid, AOEShape shape, int maxCasts = int.MaxValue, float riskyWithSecondsLeft = 0) : GenericAOEs(module, aid)
{
    public SimpleAOEs(BossModule module, ActionID aid, float radius, int maxCasts = int.MaxValue, float riskyWithSecondsLeft = 0) : this(module, aid, new AOEShapeCircle(radius), maxCasts, riskyWithSecondsLeft) { }
    public readonly AOEShape Shape = shape;
    public int MaxCasts = maxCasts; // used for staggered aoes, when showing all active would be pointless
    public uint Color; // can be customized if needed
    public bool Risky = true; // can be customized if needed
    public bool TargetIsLocation; // can be customized if needed
    public int? MaxDangerColor;
    public int? MaxRisky; // set a maximum amount of AOEs that are considered risky
    public readonly float RiskyWithSecondsLeft = riskyWithSecondsLeft; // can be used to delay risky status of AOEs, so AI waits longer to dodge, if 0 it will just use the bool Risky

    public readonly List<AOEInstance> Casters = [];

    public List<AOEInstance> ActiveCasters
    {
        get
        {
            var count = Casters.Count;
            var max = count > MaxCasts ? MaxCasts : count;
            List<AOEInstance> result = new(max);
            for (var i = 0; i < max; ++i)
            {
                result.Add(Casters[i]);
            }
            return result;
        }
    }

    public override IEnumerable<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = Casters.Count;
        if (count == 0)
            return [];
        var time = WorldState.CurrentTime;
        var max = count > MaxCasts ? MaxCasts : count;
        List<AOEInstance> result = new(max);
        for (var i = 0; i < max; ++i)
        {
            var caster = Casters[i];
            var color = i < MaxDangerColor && count > MaxDangerColor ? Colors.Danger : 0;
            var risky = Risky && (MaxRisky == null || i < MaxRisky);
            result.Add(RiskyWithSecondsLeft == 0 ? caster with { Color = color, Risky = risky }
            : caster with { Color = color, Risky = risky && caster.Activation.AddSeconds(-RiskyWithSecondsLeft) <= time });
        }
        return result;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action == WatchedAction)
            Casters.Add(new(Shape, (TargetIsLocation ? WorldState.Actors.Find(caster.CastInfo!.TargetID)?.Position : spell.LocXZ) ?? default, spell.Rotation, Module.CastFinishAt(spell), ActorID: caster.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action == WatchedAction)
        {
            for (var i = 0; i < Casters.Count; ++i)
            {
                var aoe = Casters[i];
                if (aoe.ActorID == caster.InstanceID)
                {
                    Casters.Remove(aoe);
                    break;
                }
            }
        }
    }
}

// 'charge at location' aoes that happen at the end of the cast
public class ChargeAOEs(BossModule module, ActionID aid, float halfWidth, int maxCasts = int.MaxValue, float riskyWithSecondsLeft = 0) : SimpleAOEs(module, aid, new AOEShapeCircle(default), maxCasts, riskyWithSecondsLeft)
{
    public readonly float HalfWidth = halfWidth;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action == WatchedAction)
        {
            var dir = spell.LocXZ - caster.Position;
            Casters.Add(new(new AOEShapeRect(dir.Length(), HalfWidth), caster.Position, Angle.FromDirection(dir), Module.CastFinishAt(spell), ActorID: caster.InstanceID));
        }
    }
}

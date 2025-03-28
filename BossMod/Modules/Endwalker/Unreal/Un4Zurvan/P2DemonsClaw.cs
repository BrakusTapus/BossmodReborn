﻿namespace BossMod.Endwalker.Unreal.Un4Zurvan;

class P2DemonsClawKnockback(BossModule module) : Components.GenericKnockback(module, ActionID.MakeSpell(AID.DemonsClaw), true)
{
    private Actor? _caster;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_caster?.CastInfo?.TargetID == actor.InstanceID)
            return new Knockback[1] { new(_caster.Position, 17f, Module.CastFinishAt(_caster.CastInfo)) };
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action == WatchedAction)
            _caster = caster;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action == WatchedAction)
            _caster = null;
    }
}

class P2DemonsClawWaveCannon(BossModule module) : Components.GenericWildCharge(module, 5, ActionID.MakeSpell(AID.WaveCannonShared))
{
    public Actor? Target;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action == WatchedAction)
        {
            Source = caster;
            foreach (var (slot, player) in Raid.WithSlot(false, true, true))
            {
                PlayerRoles[slot] = player == Target ? PlayerRole.Target : PlayerRole.Share;
            }
        }
        else if ((AID)spell.Action.ID == AID.DemonsClaw)
        {
            Target = WorldState.Actors.Find(spell.TargetID);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action == WatchedAction)
            Source = null;
    }
}

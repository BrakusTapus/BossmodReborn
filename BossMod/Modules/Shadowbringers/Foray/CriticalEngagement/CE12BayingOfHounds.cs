﻿namespace BossMod.Shadowbringers.Foray.CriticalEngagement.CE12BayingOfHounds;

public enum OID : uint
{
    Boss = 0x2E66, // R7.020, x1
    Hellsfire = 0x2E67, // R1.000-2.500, spawn during fight
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 6497, // Boss->player, no cast, single-target
    Hellclaw = 20534, // Boss->player, 4.0s cast, single-target, tankbuster
    TailBlow = 20535, // Boss->self, 3.0s cast, range 19 90-degree cone aoe
    LavaSpit1 = 20536, // Boss->self, 3.0s cast, single-target, visual (summon hellsfires)
    LavaSpit2 = 20537, // Boss->self, no cast, single-target, visual (summon second set of hellsfires)
    LavaSpitAOE = 20538, // Helper->location, 3.0s cast, range 5 circle
    HellsfireActivate = 19647, // Hellsfire->self, no cast, range ?-50 donut, visual (prepare for activation)
    ScorchingLash = 20553, // Hellsfire->self, 4.0s cast, range 50 width 10 rect aoe
    Hellpounce = 20539, // Boss->location, 4.0s cast, width 10 rect charge aoe, knockback away from source, dist 5 (consider showing?)
    HellpounceSecond = 20540, // Boss->location, 1.0s cast, width 10 rect charge, knockback away from source, dist 5 (consider showing?)
    LionsBreath = 20541, // Boss->self, 4.0s cast, single-target, visual (frontal cone)
    LionsBreathAOE = 20542, // Helper->self, 4.5s cast, range 60 45-degree cone aoe
    DragonsBreath = 20543, // Boss->self, 4.0s cast, single-target, visual (side cones)
    DragonsBreathAOER = 20544, // Helper->self, 4.5s cast, range 60 30-degree cone
    DragonsBreathAOEL = 20545, // Helper->self, 4.5s cast, range 60 30-degree cone
    VoidTornado = 20546, // Boss->self, 4.0s cast, single-target, visual (set hp to 1)
    VoidTornadoAOE = 20547, // Helper->self, no cast, range 30 circle, set hp to 1
    VoidQuake = 20548, // Boss->self, 3.0s cast, single-target, visual (staggered circle/donuts)
    VoidQuakeAOE1 = 20549, // Helper->self, 3.0s cast, range 10 circle aoe
    VoidQuakeAOE2 = 20550, // Helper->self, 3.0s cast, range 10-20 donut aoe
    VoidQuakeAOE3 = 20551, // Helper->self, 3.0s cast, range 20-30 donut aoe
}

class Hellclaw(BossModule module) : Components.SingleTargetCast(module, ActionID.MakeSpell(AID.Hellclaw));
class TailBlow(BossModule module) : Components.SimpleAOEs(module, ActionID.MakeSpell(AID.TailBlow), new AOEShapeCone(19, 45.Degrees()));
class LavaSpit(BossModule module) : Components.SimpleAOEs(module, ActionID.MakeSpell(AID.LavaSpitAOE), 5);
class ScorchingLash(BossModule module) : Components.SimpleAOEs(module, ActionID.MakeSpell(AID.ScorchingLash), new AOEShapeRect(50, 5));

class Hellpounce(BossModule module) : Components.GenericAOEs(module, ActionID.MakeSpell(AID.Hellpounce), "GTFO from charge!")
{
    private AOEInstance? _charge;

    public override IEnumerable<AOEInstance> ActiveAOEs(int slot, Actor actor) => Utils.ZeroOrOne(_charge);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID.Hellpounce or AID.HellpounceSecond)
            Activate(caster.Position, spell.LocXZ, Module.CastFinishAt(spell));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        switch ((AID)spell.Action.ID)
        {
            case AID.Hellpounce:
                var offset = spell.LocXZ - Arena.Center;
                Activate(spell.LocXZ, Arena.Center - offset, WorldState.FutureTime(3.7f));
                break;
            case AID.HellpounceSecond:
                _charge = null;
                break;
        }
    }

    private void Activate(WPos source, WPos target, DateTime activation)
    {
        var toTarget = target - source;
        _charge = new(new AOEShapeRect(toTarget.Length(), 5), source, Angle.FromDirection(toTarget), activation);
    }
}

abstract class Breath(BossModule module, AID aid) : Components.SimpleAOEs(module, ActionID.MakeSpell(aid), new AOEShapeCone(60, 30.Degrees()));
class DragonsBreathR(BossModule module) : Breath(module, AID.DragonsBreathAOER);
class DragonsBreathL(BossModule module) : Breath(module, AID.DragonsBreathAOEL);
class LionsBreath(BossModule module) : Breath(module, AID.LionsBreathAOE);

class VoidTornado(BossModule module) : Components.CastHint(module, ActionID.MakeSpell(AID.VoidTornado), "Set hp to 1");

class VoidQuake(BossModule module) : Components.GenericAOEs(module) // this concentric AOE can happen forwards or backwards in order with the same AID as the starter
{
    private readonly List<AOEInstance> _aoes = new(2);

    public override IEnumerable<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];
        return [_aoes[0]];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = (AID)spell.Action.ID switch
        {
            AID.VoidQuakeAOE1 => new AOEShapeCircle(10),
            AID.VoidQuakeAOE2 => new AOEShapeDonut(10, 20),
            AID.VoidQuakeAOE3 => new AOEShapeDonut(20, 30),
            _ => null
        };
        if (shape != null)
            _aoes.Add(new(shape, spell.LocXZ, default, Module.CastFinishAt(spell)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0 && (AID)spell.Action.ID is AID.VoidQuakeAOE1 or AID.VoidQuakeAOE2 or AID.VoidQuakeAOE3)
            _aoes.RemoveAt(0);
    }
}

class CE12BayingOfHoundsStates : StateMachineBuilder
{
    public CE12BayingOfHoundsStates(BossModule module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Hellclaw>()
            .ActivateOnEnter<TailBlow>()
            .ActivateOnEnter<LavaSpit>()
            .ActivateOnEnter<ScorchingLash>()
            .ActivateOnEnter<Hellpounce>()
            .ActivateOnEnter<LionsBreath>()
            .ActivateOnEnter<DragonsBreathR>()
            .ActivateOnEnter<DragonsBreathL>()
            .ActivateOnEnter<VoidTornado>()
            .ActivateOnEnter<VoidQuake>();
    }
}

[ModuleInfo(BossModuleInfo.Maturity.Verified, Contributors = "Malediktus", GroupType = BossModuleInfo.GroupType.BozjaCE, GroupID = 735, NameID = 2)] // bnpcname=9394
public class CE12BayingOfHounds(WorldState ws, Actor primary) : BossModule(ws, primary, new(154, 785), new ArenaBoundsCircle(25));

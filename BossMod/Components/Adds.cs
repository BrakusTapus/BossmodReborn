﻿namespace BossMod.Components;

// generic component used for drawing adds
public class Adds(BossModule module, uint oid, int priority = 0) : BossComponent(module)
{
    public readonly List<Actor> Actors = module.Enemies(oid);
    public IEnumerable<Actor> ActiveActors => Actors.Where(a => a.IsTargetable && !a.IsDead);

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (priority > 0)
            hints.PrioritizeTargetsByOID(oid, priority);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.Actors(Actors);
    }
}

// component for adds that shouldn't be targeted at all, but should still be drawn
public class AddsPointless(BossModule module, uint oid) : Adds(module, oid)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var act in ActiveActors)
            hints.SetPriority(act, AIHints.Enemy.PriorityPointless);
    }
}

// generic component used for drawing multiple adds with multiple oids, when it's not useful to distinguish between them
public class AddsMulti(BossModule module, uint[] oids, int priority = 0) : BossComponent(module)
{
    public readonly uint[] OIDs = oids;
    public readonly List<Actor> Actors = module.Enemies(oids);
    public IEnumerable<Actor> ActiveActors => Actors.Where(a => a.IsTargetable && !a.IsDead);

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (priority > 0)
            hints.PrioritizeTargetsByOID(OIDs, priority);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.Actors(Module.Enemies(OIDs));
    }
}

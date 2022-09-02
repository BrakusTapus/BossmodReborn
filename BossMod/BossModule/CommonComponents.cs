﻿using System.Collections.Generic;
using System.Linq;

namespace BossMod
{
    // legacy, these components should be reviewed and moved into Components dir/ns
    public static class CommonComponents
    {
        // generic 'stack to target' component that shows radius around specific actor; derived class should set actor as needed
        // it is also a cast counter
        public class FullPartyStack : Components.CastCounter
        {
            protected float StackRadius;
            protected Actor? Target;

            public FullPartyStack(ActionID aid, float stackRadius)
                : base(aid)
            {
                StackRadius = stackRadius;
            }

            public override void AddHints(BossModule module, int slot, Actor actor, TextHints hints, MovementHints? movementHints)
            {
                if (Target != null && Target != actor && !actor.Position.InCircle(Target.Position, StackRadius))
                    hints.Add("Stack!");
            }

            public override PlayerPriority CalcPriority(BossModule module, int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor)
            {
                if (Target == null)
                {
                    return PlayerPriority.Irrelevant;
                }
                else if (Target == pc)
                {
                    // other players are somewhat interesting to simplify stacking
                    return player.Position.InCircle(pc.Position, StackRadius) ? PlayerPriority.Normal : PlayerPriority.Interesting;
                }
                else
                {
                    // draw target next to which pc is to stack
                    return Target == player ? PlayerPriority.Danger : PlayerPriority.Irrelevant;
                }
            }

            public override void DrawArenaForeground(BossModule module, int pcSlot, Actor pc, MiniArena arena)
            {
                if (Target != null)
                    arena.AddCircle(Target.Position, StackRadius, ArenaColor.Danger);
            }
        }

        // generic knockback from caster component (TODO: detect knockback immunity, generalize...)
        public class KnockbackFromCaster : Components.CastCounter
        {
            public float Distance { get; private init; }
            public int MaxCasts { get; private init; } // used for staggered knockbacks, when showing all active would be pointless
            private List<Actor> _casters = new();
            public IReadOnlyList<Actor> Casters => _casters;
            public IEnumerable<Actor> ActiveCasters => _casters.Take(MaxCasts);

            public KnockbackFromCaster(ActionID aid, float distance, int maxCasts = 1)
                : base(aid)
            {
                Distance = distance;
                MaxCasts = maxCasts;
            }

            public override void AddHints(BossModule module, int slot, Actor actor, TextHints hints, MovementHints? movementHints)
            {
                if (ActiveCasters.Any(c => !module.Bounds.Contains(BossModule.AdjustPositionForKnockback(actor.Position, c, Distance))))
                    hints.Add("About to be knocked into wall!");
            }

            public override void DrawArenaForeground(BossModule module, int pcSlot, Actor pc, MiniArena arena)
            {
                foreach (var c in ActiveCasters)
                {
                    var adjPos = BossModule.AdjustPositionForKnockback(pc.Position, c, Distance);
                    arena.Actor(adjPos, pc.Rotation, ArenaColor.Danger);
                    arena.AddLine(pc.Position, adjPos, ArenaColor.Danger);
                }
            }

            public override void OnCastStarted(BossModule module, Actor caster, ActorCastInfo spell)
            {
                if (spell.Action == WatchedAction)
                    _casters.Add(caster);
            }

            public override void OnCastFinished(BossModule module, Actor caster, ActorCastInfo spell)
            {
                if (spell.Action == WatchedAction)
                    _casters.Remove(caster);
            }
        }

        // generic interrupt hint component
        public class Interruptible : BossComponent
        {
            private ActionID _watchedAction;
            private List<Actor> _casters = new();

            public Interruptible(ActionID aid)
            {
                _watchedAction = aid;
            }

            public override void AddGlobalHints(BossModule module, GlobalHints hints)
            {
                if (_casters.Count > 0)
                {
                    hints.Add("Interrupt!");
                }
            }

            public override void OnCastStarted(BossModule module, Actor caster, ActorCastInfo spell)
            {
                if (spell.Action == _watchedAction)
                {
                    _casters.Add(caster);
                }
            }

            public override void OnCastFinished(BossModule module, Actor caster, ActorCastInfo spell)
            {
                if (spell.Action == _watchedAction)
                {
                    _casters.Remove(caster);
                }
            }
        }
    }
}

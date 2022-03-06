﻿using System.Linq;
using System.Numerics;

namespace BossMod.P3S
{
    using static BossModule;

    // state related to trail of condemnation mechanic
    class TrailOfCondemnation : Component
    {
        public bool Done { get; private set; } = false;
        private P3S _module;
        private bool _isCenter;

        private static float _halfWidth = 7.5f;
        private static float _sidesOffset = 12.5f;
        private static float _aoeRadius = 6;

        public TrailOfCondemnation(P3S module, bool center)
        {
            _module = module;
            _isCenter = center;
        }

        public override void AddHints(int slot, Actor actor, TextHints hints, MovementHints? movementHints)
        {
            var boss = _module.Boss();
            if (boss == null || boss.Position == _module.Arena.WorldCenter)
                return;

            var dir = Vector3.Normalize(_module.Arena.WorldCenter - boss.Position);
            if (_isCenter)
            {
                if (GeometryUtils.PointInRect(actor.Position - boss.Position, dir, 2 * _module.Arena.WorldHalfSize, 0, _halfWidth))
                {
                    hints.Add("GTFO from aoe!");
                }
                if (_module.Raid.WithoutSlot().InRadiusExcluding(actor, _aoeRadius).Any())
                {
                    hints.Add("Spread!");
                }
            }
            else
            {
                var offset = _sidesOffset * new Vector3(-dir.Z, 0, dir.X);
                if (GeometryUtils.PointInRect(actor.Position - boss.Position + offset, dir, 2 * _module.Arena.WorldHalfSize, 0, _halfWidth) ||
                    GeometryUtils.PointInRect(actor.Position - boss.Position - offset, dir, 2 * _module.Arena.WorldHalfSize, 0, _halfWidth))
                {
                    hints.Add("GTFO from aoe!");
                }
                // note: sparks either target all tanks & healers or all dds - so correct pairings are always dd+tank/healer
                int numStacked = 0;
                bool goodPair = false;
                foreach (var pair in _module.Raid.WithoutSlot().InRadiusExcluding(actor, _aoeRadius))
                {
                    ++numStacked;
                    goodPair = (actor.Role == Role.Tank || actor.Role == Role.Healer) != (pair.Role == Role.Tank || pair.Role == Role.Healer);
                }
                if (numStacked != 1)
                {
                    hints.Add("Stack in pairs!");
                }
                else if (!goodPair)
                {
                    hints.Add("Incorrect pairing!");
                }
            }
        }

        public override void DrawArenaBackground(MiniArena arena)
        {
            var boss = _module.Boss();
            if (boss == null || boss.Position == arena.WorldCenter)
                return;

            var dir = Vector3.Normalize(arena.WorldCenter - boss.Position);
            if (_isCenter)
            {
                arena.ZoneQuad(boss.Position, dir, 2 * arena.WorldHalfSize, 0, _halfWidth, arena.ColorAOE);
            }
            else
            {
                var offset = _sidesOffset * new Vector3(-dir.Z, 0, dir.X);
                arena.ZoneQuad(boss.Position + offset, dir, 2 * arena.WorldHalfSize, 0, _halfWidth, arena.ColorAOE);
                arena.ZoneQuad(boss.Position - offset, dir, 2 * arena.WorldHalfSize, 0, _halfWidth, arena.ColorAOE);
            }
        }

        public override void DrawArenaForeground(MiniArena arena)
        {
            var pc = _module.Player();
            if (pc == null)
                return;

            // draw all raid members, to simplify positioning
            foreach (var player in _module.Raid.WithoutSlot().Exclude(pc))
            {
                bool inRange = GeometryUtils.PointInCircle(player.Position - pc.Position, _aoeRadius);
                arena.Actor(player, inRange ? arena.ColorPlayerInteresting : arena.ColorPlayerGeneric);
            }

            // draw circle around pc
            arena.AddCircle(pc.Position, _aoeRadius, arena.ColorDanger);
        }

        public override void OnEventCast(CastEvent info)
        {
            if (info.IsSpell(AID.FlareOfCondemnation) || info.IsSpell(AID.SparksOfCondemnation))
                Done = true;
        }
    }
}

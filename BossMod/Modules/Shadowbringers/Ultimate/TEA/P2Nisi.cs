﻿using System;

namespace BossMod.Shadowbringers.Ultimate.TEA
{
    class P2Nisi : BossComponent
    {
        public enum Nisi { None, Alpha, Beta, Gamma, Delta }

        public int ShowPassHint; // show hints for Nth pass
        private int _numNisiApplications;
        private int[] _partners = new int[PartyState.MaxPartySize];
        private Nisi[] _current = new Nisi[PartyState.MaxPartySize];
        private Nisi[] _judgments = new Nisi[PartyState.MaxPartySize];

        public override void Init(BossModule module)
        {
            Array.Fill(_partners, -1);
            int[] firstMembersOfGroup = { -1, -1, -1, -1 };
            foreach (var p in Service.Config.Get<TEAConfig>().P2NisiPairs.Resolve(module.Raid))
            {
                ref var partner = ref firstMembersOfGroup[p.group];
                if (partner < 0)
                {
                    partner = p.slot;
                }
                else
                {
                    _partners[p.slot] = partner;
                    _partners[partner] = p.slot;
                }
            }
        }

        public override void AddHints(BossModule module, int slot, Actor actor, TextHints hints, MovementHints? movementHints)
        {
            if (PassPartnerSlot(slot) >= 0)
            {
                hints.Add("Pass nisi!");
            }
        }

        public override void DrawArenaForeground(BossModule module, int pcSlot, Actor pc, MiniArena arena)
        {
            var partner = module.Raid[PassPartnerSlot(pcSlot)];
            if (partner != null)
            {
                arena.AddLine(pc.Position, partner.Position, ArenaColor.Danger);
            }
        }

        public override void OnStatusGain(BossModule module, Actor actor, ActorStatus status)
        {
            var nisi = NisiForSID((SID)status.ID);
            if (nisi != Nisi.None && module.Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            {
                ++_numNisiApplications;
                _current[slot] = nisi;
            }

            var judgment = (SID)status.ID switch
            {
                SID.FinalJudgmentNisiAlpha => Nisi.Alpha,
                SID.FinalJudgmentNisiBeta => Nisi.Beta,
                SID.FinalJudgmentNisiGamma => Nisi.Gamma,
                SID.FinalJudgmentNisiDelta => Nisi.Delta,
                _ => Nisi.None
            };
            if (judgment != Nisi.None && module.Raid.FindSlot(actor.InstanceID) is var judgmentSlot && judgmentSlot >= 0)
            {
                _judgments[judgmentSlot] = judgment;
            }
        }

        public override void OnStatusLose(BossModule module, Actor actor, ActorStatus status)
        {
            var nisi = NisiForSID((SID)status.ID);
            if (nisi != Nisi.None && module.Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0 && nisi == _current[slot])
                _current[slot] = Nisi.None;
        }

        private Nisi NisiForSID(SID sid) => sid switch
        {
            SID.FinalDecreeNisiAlpha => Nisi.Alpha,
            SID.FinalDecreeNisiBeta => Nisi.Beta,
            SID.FinalDecreeNisiGamma => Nisi.Gamma,
            SID.FinalDecreeNisiDelta => Nisi.Delta,
            _ => Nisi.None
        };

        private int PassPartnerSlot(int slot)
        {
            if (_numNisiApplications < 4)
                return -1; // initial nisi not applied yet
            if (_numNisiApplications >= 4 + 4 * ShowPassHint)
                return -1; // expected nisi passes are done

            var partner = _partners[slot]; // by default use assigned partner (first two passes before judgments)
            if (_judgments[slot] != Nisi.None)
            {
                if (_current[slot] == Nisi.None)
                {
                    // we need to grab correct nisi to match our judgment
                    partner = Array.IndexOf(_current, _judgments[slot]);
                }
                else
                {
                    // we need to pass nisi to whoever has correct judgment
                    partner = Array.IndexOf(_judgments, _current[slot]);
                }
            }

            if (partner < 0)
                return -1; // partner not assigned correctly
            if (_current[slot] != Nisi.None && _current[partner] != Nisi.None)
                return -1; // both partners have nisi already
            return partner;
        }
    }
}
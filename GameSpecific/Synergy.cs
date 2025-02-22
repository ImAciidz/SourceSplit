﻿using LiveSplit.ComponentUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiveSplit.SourceSplit.GameHandling;
using LiveSplit.SourceSplit.Utilities;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class Synergy : GameSupport
    {
        // how to match with demos:
        // start: on map load

        private CustomCommand _autosplitIL = new CustomCommand("ilstart", "0");
        private CustomCommandHandler _cmdHandler;
        private const FL _dead = FL.ATCONTROLS | FL.NOTARGET | FL.AIMTARGET;

        private HL2 _hl2 = new HL2();
        private HL2Ep1 _ep1 = new HL2Ep1();
        private HL2Ep2 _ep2 = new HL2Ep2();

        public Synergy()
        {
            
            _cmdHandler = new CustomCommandHandler(_autosplitIL);
            this.AdditionalGameSupport.AddRange(new GameSupport[] { _hl2 , _ep1 , _ep2 });
        }

        public override void OnGameAttached(GameState state, TimerActions actions)
        {
            _cmdHandler.Init(state);
        }

        public override void OnGenericUpdate(GameState state, TimerActions actions)
        {
            _cmdHandler.Update(state);

            // HACKHACK: when the player dies and respawn, the map is also lightly reloaded,
            // potentially causing all the entity indicies to change
            // players when killed also have these flags applied to them and removed when respawning
            // so lets fire onsessionstart then
            if (state.PlayerEntInfo.EntityPtr != IntPtr.Zero)
            {
                if (!state.PlayerFlags.Current.HasFlag(_dead) &&
                    state.PlayerFlags.Old.HasFlag(_dead))
                {
                    this.OnSessionStart(state, actions);
                    this.AdditionalGameSupport.ForEach(x => x.OnSessionStart(state, actions));
                    Debug.WriteLine("synergy session start");
                }    
            }

            if (_autosplitIL.BValue)
            {
                if (!StartOnFirstLoadMaps.Contains(state.Map.Current))
                {
                    StartOnFirstLoadMaps.Clear();
                    StartOnFirstLoadMaps.Add(state.Map.Current);
                }
            }
            else
                StartOnFirstLoadMaps.Clear();
        }

        public override void OnUpdate(GameState state, TimerActions actions)
        {
            _cmdHandler.Update(state);
            return;
        }
    }
}
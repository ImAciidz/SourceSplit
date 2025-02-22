﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiveSplit.ComponentUtil;
using LiveSplit.SourceSplit.GameHandling;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class HL2Ep2 : GameSupport
    {
        // how to match this timing with demos:
        // start: 
        // ending: the tick where velocity changes from 600.X to 0.0 AFTER the camera effects (cl_showpos 1)

        private bool _onceFlag;
        private int _basePlayerLaggedMovementOffset = -1;
        private float _prevLaggedMovementValue;

        private HL2Mods_DarkIntervention _darkIntervention = new HL2Mods_DarkIntervention();
        private HL2Mods_HellsMines _hellsMines = new HL2Mods_HellsMines();
        private HL2Mods_UpmineStruggle _upmineStruggle = new HL2Mods_UpmineStruggle();

        public HL2Ep2()
        {
            this.AddFirstMap("ep2_outland_01");
            this.AddLastMap("ep2_outland_12a");
            AdditionalGameSupport = new List<GameSupport>(){ _darkIntervention, _hellsMines, _upmineStruggle };
        }

        public override void OnGameAttached(GameState state, TimerActions actions)
        {
            base.OnGameAttached(state, actions);

            ProcessModuleWow64Safe server = state.GetModule("server.dll");
            var scanner = new SignatureScanner(state.GameProcess, server.BaseAddress, server.ModuleMemorySize);

            if (GameMemory.GetBaseEntityMemberOffset("m_flLaggedMovementValue", state.GameProcess, scanner, out _basePlayerLaggedMovementOffset))
                Debug.WriteLine("CBasePlayer::m_flLaggedMovementValue offset = 0x" + _basePlayerLaggedMovementOffset.ToString("X"));
        }

        public override void OnSessionStart(GameState state, TimerActions actions)
        {
            base.OnSessionStart(state, actions);

            if (state.PlayerEntInfo.EntityPtr != IntPtr.Zero && _basePlayerLaggedMovementOffset != -1)
                state.GameProcess.ReadValue(state.PlayerEntInfo.EntityPtr + _basePlayerLaggedMovementOffset, out _prevLaggedMovementValue);

            _onceFlag = false;
        }

        public override void OnUpdate(GameState state, TimerActions actions)
        {
            if (_onceFlag)
                return;

            if (this.IsFirstMap && state.PlayerEntInfo.EntityPtr != IntPtr.Zero && _basePlayerLaggedMovementOffset != -1)
            {
                // "OnMapSpawn" "startcar_speedmod,ModifySpeed,0,0,-1"
                // "OnMapSpawn" "startcar_speedmod,ModifySpeed,1,12.5,-1"

                float laggedMovementValue;
                state.GameProcess.ReadValue(state.PlayerEntInfo.EntityPtr + _basePlayerLaggedMovementOffset, out laggedMovementValue);

                if (laggedMovementValue.BitEquals(1.0f) && !_prevLaggedMovementValue.BitEquals(1.0f))
                {
                    Debug.WriteLine("ep2 start");
                    _onceFlag = true;
                    actions.Start(StartOffsetTicks); return;
                }

                _prevLaggedMovementValue = laggedMovementValue;
            }
            else if (this.IsLastMap)
            {
                // "OnTrigger4" "cvehicle.hangar,EnterVehicle,,0,1"

                if (state.PlayerParentEntityHandle.Current != -1
                    && state.PlayerParentEntityHandle.Old == -1)
                {
                    Debug.WriteLine("ep2 end");
                    _onceFlag = true;
                    EndOffsetTicks = 0;
                    actions.End(EndOffsetTicks); return;
                }
            }

            return;
        }
    }
}

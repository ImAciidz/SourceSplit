﻿using System;
using System.Diagnostics;
using System.Linq;
using LiveSplit.ComponentUtil;
using LiveSplit.SourceSplit.GameHandling;
using LiveSplit.SourceSplit.Utilities;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class PortalMods_TheFlashVersion : PortalBase
    {
        // how to match with demos:
        // start: first tick when your position is at 0 168 129 (cl_showpos 1)
        // ending: first tick player is slowed down by the ending trigger

        private bool _onceFlag;
        private Vector3f _startPos = new Vector3f(0f, 168f, 129f);
        private int _laggedMovementOffset = -1;
        private const int VAULT_SAVE_TICK = 3876;

        public PortalMods_TheFlashVersion() : base()
        {
            this.AddFirstMap("portaltfv1");
            this.AddLastMap("portaltfv5");
        }

        public override void OnGameAttached(GameState state, TimerActions actions)
        {
            ProcessModuleWow64Safe server = state.GameProcess.ModulesWow64SafeNoCache().FirstOrDefault(x => x.ModuleName.ToLower() == "server.dll");
            Trace.Assert(server != null);

            var scanner = new SignatureScanner(state.GameProcess, server.BaseAddress, server.ModuleMemorySize);

            if (GameMemory.GetBaseEntityMemberOffset("m_flLaggedMovementValue", state.GameProcess, scanner, out _laggedMovementOffset))
                Debug.WriteLine("CBasePlayer::m_flLaggedMovementValue offset = 0x" + _laggedMovementOffset.ToString("X"));
        }

        public override void OnSessionStart(GameState state, TimerActions actions)
        {
            base.OnSessionStart(state, actions);
            _onceFlag = false;
        }

        public override void OnUpdate(GameState state, TimerActions actions)
        {
            if (_onceFlag)
                return;

            if (this.IsFirstMap)
            {
                // vault save starts at tick 3876, but update interval may miss it so be a little lenient
                if ((state.TickBase >= VAULT_SAVE_TICK && state.TickBase <= VAULT_SAVE_TICK + 4))
                {
                    Debug.WriteLine("tfv start");
                    _onceFlag = true;
                    int ticksSinceVaultSaveTick = state.TickBase - VAULT_SAVE_TICK; // account for missing ticks if update interval missed it
                    StartOffsetTicks = -3803 - ticksSinceVaultSaveTick; // 57.045 seconds
                    actions.Start(StartOffsetTicks); return;
                }

                // map started without vault save
                else if (state.PlayerPosition.Current.DistanceXY(_startPos) < 1.0f)
                {
                    Debug.WriteLine("tfv start");
                    _onceFlag = true;
                    actions.Start(StartOffsetTicks); return;
                }
            }
            else if (this.IsLastMap && state.PlayerEntInfo.EntityPtr != IntPtr.Zero)
            {
                // "OnTrigger" "weapon_disable2:ModifySpeed:0.4:0:-1"
                float laggedMovementValue;
                state.GameProcess.ReadValue(state.PlayerEntInfo.EntityPtr + _laggedMovementOffset, out laggedMovementValue);
                if (laggedMovementValue == 0.4f)
                {
                    Debug.WriteLine("tfv end");
                    _onceFlag = true;
                    actions.End(EndOffsetTicks); return;
                }
            }

            return;
        }
    }
}

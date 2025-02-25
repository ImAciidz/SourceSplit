﻿using LiveSplit.ComponentUtil;
using System.Diagnostics;
using LiveSplit.SourceSplit.GameHandling;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class HL2Mods_GGEFC13 : GameSupport
    {
        // start: on input to teleport the player
        // ending: when the helicopter's hp drops to 0 or lower

        private bool _onceFlag;
        private float _splitTime;
        private int _baseEntityHealthOffset = -1;
        private MemoryWatcher<int> _heliHP;

        public HL2Mods_GGEFC13()
        {
            this.AddFirstMap("ge_city01");
            this.AddLastMap("ge_final");
        }

        public override void OnGameAttached(GameState state, TimerActions actions)
        {
            ProcessModuleWow64Safe server = state.GetModule("server.dll");

            var scanner = new SignatureScanner(state.GameProcess, server.BaseAddress, server.ModuleMemorySize);

            if (GameMemory.GetBaseEntityMemberOffset("m_iHealth", state.GameProcess, scanner, out _baseEntityHealthOffset))
                Debug.WriteLine("CBaseEntity::m_iHealth offset = 0x" + _baseEntityHealthOffset.ToString("X"));
        }

        public override void OnSessionStart(GameState state, TimerActions actions)
        {
            base.OnSessionStart(state, actions);

            if (this.IsFirstMap)
                _splitTime = state.GameEngine.GetOutputFireTime("teleport_trigger");
            else if (this.IsLastMap)
                _heliHP = new MemoryWatcher<int>(state.GameEngine.GetEntityByName("helicopter") + _baseEntityHealthOffset);

            _onceFlag = false;
        }

        public override void OnUpdate(GameState state, TimerActions actions)
        {
            if (_onceFlag)
                return;

            if (this.IsFirstMap)
            {
                float splitTime = state.GameEngine.GetOutputFireTime("teleport_trigger", 5);
                if (splitTime == 0 && _splitTime != 0)
                {
                    Debug.WriteLine("ggefc13 start");
                    _onceFlag = true;
                    actions.Start(StartOffsetTicks);
                }

                _splitTime = splitTime;
            }
            else if (this.IsLastMap)
            {
                _heliHP.Update(state.GameProcess);

                if (_heliHP.Current <= 0 && _heliHP.Old > 0)
                {
                    Debug.WriteLine("ggefc13 end");
                    _onceFlag = true;
                    actions.End(EndOffsetTicks); return;
                }
            }
            return;
        }
    }
}

﻿using LiveSplit.ComponentUtil;
using System;
using System.Diagnostics;
using System.Linq;
using LiveSplit.SourceSplit.GameHandling;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class HL2Mods_YearLongAlarm : GameSupport
    {
        // start: on first map
        // ending: when one of the 2 gunships' HP drops from 0 to lower

        private bool _onceFlag;
        private int _baseEntityHealthOffset = -1;

        // there are 2 gunships, each of them killed will count as a timer end. one of them spawns at level start and is later deleted

        // gunships' old hp
        private int[] _gunshipOldHP = new int[] { -1, 100 };
        // gunships' current hp
        private int[] _gunshipHP = new int[] { -1, 100 };
        // gunships' index, used for searching their pointers
        private int[] _gunshipIndex = new int[] { -1, -1 };
        // gunships' names, used for searching for their indices
        private string[] _gunshipName = new string[] { "gunship", "gunship_intro" };

        public HL2Mods_YearLongAlarm()
        {
            this.AddFirstMap("yla_mine");
            this.AddLastMap("yla_bridge");
            this.StartOnFirstLoadMaps.AddRange(this.FirstMaps);
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
            if (IsLastMap && _baseEntityHealthOffset != -1)
            {
                for (int i = 0; i <= 1; i++)
                {
                    // get the gunships' indicies
                    _gunshipIndex[i] = state.GameEngine.GetEntIndexByName(_gunshipName[i]);

                    // and decide their hp
                    if (_gunshipIndex[i] != -1)
                        _gunshipHP[i] = state.GameProcess.ReadValue<int>(state.GameEngine.GetEntInfoByIndex(_gunshipIndex[i]).EntityPtr + _baseEntityHealthOffset);
                    else 
                        _gunshipHP[i] = -1;

                    Debug.WriteLine(_gunshipName[i] + " index is " + _gunshipIndex[i]);
                }

            }
            _onceFlag = false;
        }


        public override void OnUpdate(GameState state, TimerActions actions)
        {
            if (_onceFlag)
                return;

            if (this.IsLastMap)
            {
                // check if the trigger that spawns the 2nd gunship has been triggered, if so, check for its pointer
                if (_gunshipIndex[0] == -1 && state.GameEngine.GetEntIndexByPos(-13.39f, 227.25f, 393.67f) == -1)
                    _gunshipIndex[0] = state.GameEngine.GetEntIndexByName(_gunshipName[0]);

                for (int i = 0; i <= 1; i++)
                {
                    // store the old hp
                    _gunshipOldHP[i] = _gunshipHP[i];
                    // get the gunship's pointer
                    IntPtr ptr = state.GameEngine.GetEntInfoByIndex(_gunshipIndex[i]).EntityPtr;
                    // if the gunship hasn't spawned in yet or they're deleted, exit early and reset its old index
                    if (_gunshipIndex[i] == -1 || ptr == IntPtr.Zero)
                    {
                        _gunshipIndex[i] = -1;
                        continue;
                    }
                    else
                    {
                        // get the new hp
                        _gunshipHP[i] = state.GameProcess.ReadValue<int>(ptr + _baseEntityHealthOffset);
                        // now compare
                        if (_gunshipOldHP[i] > 0 && _gunshipHP[i] <= 0)
                        {
                            Debug.WriteLine("year long alarm end");
                            Debug.WriteLine(_gunshipName[i] + " died at hp " + _gunshipHP[i] + " and old hp " + _gunshipOldHP[i]);
                            _onceFlag = true;
                            actions.End(EndOffsetTicks); return;
                        }
                    }
                }
            }

            return;
        }
    }
}

﻿using LiveSplit.ComponentUtil;
using System;
using System.Diagnostics;
using System.Linq;
using LiveSplit.SourceSplit.GameHandling;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class HL2Mods_Ptsd2 : GameSupport
    {
        // how to match with demos:
        // start: after output to unfreeze player is fired
        // ending: when the byte for if a video is playing turns from 0 to 1

        private bool _onceFlag;

        private float _splitTime;
        private MemoryWatcher<byte> _videoPlaying;

        public HL2Mods_Ptsd2()
        {
            this.AddFirstMap("ptsd_2_p1");
            this.AddLastMap("ptsd_2_final_day");
        }

        public override void OnGameAttached(GameState state, TimerActions actions)
        {
            var bink = state.GetModule("video_bink.dll");
            Trace.Assert(bink != null);

            var binkScanner = new SignatureScanner(state.GameProcess, bink.BaseAddress, bink.ModuleMemorySize);

            SigScanTarget target = new SigScanTarget(11, "C7 05 ?? ?? ?? ?? ?? ?? ?? ?? B9 ?? ?? ?? ??");
            target.OnFound = (proc, scanner, ptr) => {
                ptr = proc.ReadPointer(ptr) + 0xC;
                Debug.WriteLine("bink is video playing pointer found at 0x" + ptr.ToString("X"));
                return ptr;
            };

            _videoPlaying = new MemoryWatcher<byte>(binkScanner.Scan(target));
        }

        public override void OnSessionStart(GameState state, TimerActions actions)
        {
            base.OnSessionStart(state, actions);

            if (this.IsFirstMap)
                _splitTime = state.GameEngine.GetOutputFireTime("scream", "PlaySound", "", 5);

            _onceFlag = false;
        }

        public override void OnGenericUpdate(GameState state, TimerActions actions)
        {
            if (this.IsLastMap)
            {
                _videoPlaying.Update(state.GameProcess);

                if (_videoPlaying.Old == 0 && _videoPlaying.Current == 1)
                {
                    Debug.WriteLine("ptsd end");
                    _onceFlag = true;
                    state.QueueOnNextSessionEnd = () => actions.End(EndOffsetTicks);
                }
            }
        }

        public override void OnUpdate(GameState state, TimerActions actions)
        {
            if (_onceFlag)
                return;

            if (this.IsFirstMap)
            {
                float splitTime = state.GameEngine.GetOutputFireTime("scream", "PlaySound", "", 5);

                if (splitTime == 0f && _splitTime != 0f)
                {
                    Debug.WriteLine("ptsd start");
                    _onceFlag = true;
                    _splitTime = splitTime;
                    actions.Start(StartOffsetTicks); return;
                }

                _splitTime = splitTime;
            }
            return;
        }
    }
}

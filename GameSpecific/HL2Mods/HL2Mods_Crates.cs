﻿using LiveSplit.ComponentUtil;
using System.Diagnostics;
using LiveSplit.SourceSplit.GameHandling;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class HL2Mods_TooManyCrates : GameSupport
    {
        // start: on first map
        // ending: when the end text model's skin code is 10 and player view entity switches to the final camera

        private bool _onceFlag;

        private MemoryWatcher<int> _counterSkin;
        private int _camIndex;

        private const int _baseSkinOffset = 872;

        public HL2Mods_TooManyCrates()
        {
            this.AddFirstMap("cratastrophy");
            this.StartOnFirstLoadMaps.AddRange(this.FirstMaps);
        }

        public override void OnSessionStart(GameState state, TimerActions actions)
        {
            base.OnSessionStart(state, actions);
            if (IsFirstMap)
            {
                _counterSkin = new MemoryWatcher<int>(state.GameEngine.GetEntityByName("EndWords") + _baseSkinOffset);
                _camIndex = state.GameEngine.GetEntIndexByName("EndCamera");
                //Debug.WriteLine("found end cam index at " + _camIndex);
            }
            _onceFlag = false;
        }


        public override void OnUpdate(GameState state, TimerActions actions)
        {
            if (_onceFlag)
                return;

            if (this.IsFirstMap)
            {
                _counterSkin.Update(state.GameProcess);
                if (_counterSkin.Current == 10 && state.PlayerViewEntityIndex.Current == _camIndex && state.PlayerViewEntityIndex.Old == 1)
                {
                    _onceFlag = true;
                    Debug.WriteLine("toomanycrates end");
                    actions.End(EndOffsetTicks); return;
                }
            }

            return;
        }
    }
}

﻿using System.Diagnostics;
using LiveSplit.SourceSplit.GameHandling;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class HL2Mods_DaBaby : GameSupport
    {
        // start: on first map
        // ending: when the player's view entity index changes to ending camera's

        private bool _onceFlag;

        private int _endingCamIndex;
        private int _startCamIndex;

        public HL2Mods_DaBaby()
        {
            this.AddFirstMap("dababy_hallway_ai");  
        }

        public override void OnSessionStart(GameState state, TimerActions actions)
        {
            base.OnSessionStart(state, actions);

            if (this.IsFirstMap)
            {
                _endingCamIndex = state.GameEngine.GetEntIndexByName("final_viewcontrol");
                _startCamIndex = state.GameEngine.GetEntIndexByName("viewcontrol");
                //Debug.WriteLine($"found start cam index at {_startCamIndex} and end cam at {_endingCamIndex}");
            }

            _onceFlag = false;
        }

        public override void OnUpdate(GameState state, TimerActions actions)
        {
            if (_onceFlag)
                return;

            if (_startCamIndex != -1)
            {
                if (state.PlayerViewEntityIndex.Current == 1 && state.PlayerViewEntityIndex.Old == _startCamIndex)
                {
                    Debug.WriteLine("da baby start");
                    actions.Start(StartOffsetTicks); return;
                }
            }

            if (_endingCamIndex != -1)
            {
                if (state.PlayerViewEntityIndex.Current == _endingCamIndex && state.PlayerViewEntityIndex.Old == 1)
                {
                    Debug.WriteLine("da baby end");
                    _onceFlag = true;
                    actions.End(EndOffsetTicks); return;
                }
            }
            return;
        }
    }
}

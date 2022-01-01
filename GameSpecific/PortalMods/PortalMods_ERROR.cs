﻿using System.Diagnostics;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class PortalMods_ERROR : GameSupport
    {
        // how to match this timing with demos:
        // start: on first map load
        // ending: on game disconnect

        private int _startCamIndex;
        private int _endCamIndex;
        private bool _onceFlag;

        public PortalMods_ERROR()
        {
            this.GameTimingMethod = GameTimingMethod.EngineTicksWithPauses;
            this.AddFirstMap("err1");
            this.AddLastMap("err18");
            this.RequiredProperties = PlayerProperties.ViewEntity;
        }

        public override void OnSessionStart(GameState state)
        {
            base.OnSessionStart(state);

            if (IsFirstMap)
            {
                _startCamIndex = state.GetEntIndexByName("blackout_viewcontroller");
                Debug.WriteLine($"start cam idex is {_startCamIndex}");
            }
            else if (IsLastMap)
            {
                _endCamIndex = state.GetEntIndexByName("cutscene_camera");
                Debug.WriteLine($"start cam idex is {_endCamIndex}");
            }
            _onceFlag = false;
        }

        public override GameSupportResult OnUpdate(GameState state)
        {
            if (_onceFlag)
                return GameSupportResult.DoNothing;

            if (IsFirstMap)
            {
                if (state.PlayerViewEntityIndex.Old == _startCamIndex && state.PlayerViewEntityIndex.Current == 1)
                    return GameSupportResult.PlayerGainedControl;
            }
            else if (IsLastMap)
            {
                if (state.PlayerViewEntityIndex.Old == 1 && state.PlayerViewEntityIndex.Current == _endCamIndex)
                    return GameSupportResult.PlayerLostControl;
            }

            return GameSupportResult.DoNothing;
        }

    }
}

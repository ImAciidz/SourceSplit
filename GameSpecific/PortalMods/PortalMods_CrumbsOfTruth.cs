﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiveSplit.ComponentUtil;
using LiveSplit.SourceSplit.Utilities;
using LiveSplit.SourceSplit.GameHandling;
using System.IO;
using System.Security.Cryptography;

namespace LiveSplit.SourceSplit.GameSpecific
{
    class PortalMods_CrumbsOfTruth : PortalBase
    {
        // how to match this timing with demos:
        // start: when view entity changes from the camera's
        // ending: (achieved using map transition)

        public PortalMods_CrumbsOfTruth() : base()
        {
            this.AddFirstMap("rickychamber_intro");
        }

        public override void OnSaveLoaded(GameState state, TimerActions actions, string name)
        {
            base.OnSaveLoaded(state, actions, name);

            var path = Path.Combine(state.AbsoluteGameDir, "SAVE", name + ".sav");
            string md5 = FileUtils.GetMD5(path);

            if (md5 == "c6c02f3fd37234f67115c67f3416a0c4")
            {
                actions.Start(0);
                Debug.WriteLine($"portal cot vault save start");
                return;
            }
        }

    }
}

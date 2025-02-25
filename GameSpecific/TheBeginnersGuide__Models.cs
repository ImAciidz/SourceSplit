﻿using LiveSplit.ComponentUtil;
using System;
using System.Diagnostics;
using LiveSplit.SourceSplit.GameHandling;
using LiveSplit.SourceSplit.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static LiveSplit.SourceSplit.GameSpecific.TBGFireTimeWatcher;

namespace LiveSplit.SourceSplit.GameSpecific
{

    internal interface ITBGExtraSplit
    {
        String Description { get; set; }
        string Map { get; set; }

        bool CheckSplit(GameState state);
        void Update(GameState state);
        void Reset(GameState state);
    }

    internal class TBGValueWatcher<T> : ValueWatcher<T>, ITBGExtraSplit where T: struct
    {
        private protected Func<GameState, T> _getFunc;

        private protected T? _from = null;
        private protected T? _to = null;

        public TBGValueWatcher(T t) : base(t) { }

        public TBGValueWatcher(T t, Func<GameState, T> getFunc, T? from, T? to) : base(t)
        {
            _getFunc = getFunc ;
            _from = from;
            _to = to;
        }

        public string Map { get; set; } = "";
        public string Description { get; set; } = "";

        public void Reset(GameState state)
        {
            base.Reset(_getFunc(state));
        }

        public void Update(GameState state)
        {
            Current = _getFunc(state);
        }

        public virtual bool CheckSplit(GameState state)
        {
            if (_from != null && _to != null)
                return ChangedFromTo(_from.Value, _to.Value);
            if (_from == null && _to != null)
                return ChangedTo(_to.Value);
            if (_from != null && _to == null)
                return ChangedFrom(_from.Value);

            return false;
        }
    }
    internal class TBGFireTimeWatcher : TBGValueWatcher<float>
    {
        public TBGFireTimeWatcher(OutputFireType type,
            string targetEntName, string command = null, string param = null) : base(0)
        {
            _getFunc = (command == null && param == null) ?
                (s) => s.GameEngine.GetOutputFireTime(targetEntName) :
                (s) => s.GameEngine.GetOutputFireTime(targetEntName, command, param);

            switch (type)
            {
                case OutputFireType.FinishedDelay: _to = 0; break;
                case OutputFireType.BeganDelay: _from = 0; break;
            }
        }

        public enum OutputFireType
        {
            BeganDelay,
            FinishedDelay,
        }
    }

    internal abstract class TBGEntityIndex : ITBGExtraSplit
    {
        public string Description { get; set; }
        public string Map { get; set; }

        private protected int _index = -1;
        private protected Func<GameState, int> _getIndex;


        public TBGEntityIndex(string name)
            => _getIndex = (s) => s.GameEngine.GetEntIndexByName(name);

        public TBGEntityIndex(Vector3f pos)
            => _getIndex = (s) => s.GameEngine.GetEntIndexByPos(pos, 1 / 16f);

        public abstract bool CheckSplit(GameState state);

        public virtual void Reset(GameState state)
        {
            _index = _getIndex(state);
        }

        public virtual void Update(GameState state) {; }
    }
    internal class TBGEntityDeletion : TBGEntityIndex
    {
        public TBGEntityDeletion(string name) : base(name) { }
        public TBGEntityDeletion(Vector3f pos) : base(pos) { }

        private bool _split = false;

        public override void Update(GameState state)
        {
            _split = false;

            if (_index == -1) return;
            var ent = state.GameEngine.GetEntInfoByIndex(_index);

            if (ent.EntityPtr == IntPtr.Zero)
            {
                _index = -1;
                _split = true;
            }
        }

        public override bool CheckSplit(GameState state)
        {
            return _split;
        }
    }
    internal class TBGPlayerViewEntity : TBGEntityIndex
    {
        public enum PlayerViewEntityChangeType
        {
            ChangedTo,
            ChangedFrom
        }
        private PlayerViewEntityChangeType _type;

        public TBGPlayerViewEntity(PlayerViewEntityChangeType type, string name) : base(name) { _type = type; }
        public TBGPlayerViewEntity(PlayerViewEntityChangeType type, Vector3f pos) : base(pos) { _type = type; }

        public override bool CheckSplit(GameState state)
        {
            if (_index == -1) return false;
            return _type switch
            {
                PlayerViewEntityChangeType.ChangedFrom => state.PlayerViewEntityIndex.ChangedFrom(_index),
                PlayerViewEntityChangeType.ChangedTo => state.PlayerViewEntityIndex.ChangedTo(_index),
                _ => false
            };
        }
    }
    internal class TBGEntityState : TBGEntityIndex
    {
        private TBGEntityStateChangeType _type;
        private int _offset = -1;
        private TBGValueWatcher<bool> _enabled;
        public TBGEntityState(string name, TBGEntityStateChangeType type, int offset) : base(name)
        {
            _type = type;
            _offset = offset;

            Reset(null);
        }

        public override void Reset(GameState state)
        {
            if (state != null)
                base.Reset(state);

            Func<GameState, bool> act = (s) => s.GameProcess.ReadValue<bool>(state.GameEngine.GetEntInfoByIndex(_index).EntityPtr + _offset);
            switch (_type)
            {
                case TBGEntityStateChangeType.Enabled:
                    _enabled = new TBGValueWatcher<bool>(false, act, false, true);
                    break;
                case TBGEntityStateChangeType.Disabled:
                    _enabled = new TBGValueWatcher<bool>(false, act, true, false);
                    break;
            }
        }
        
        public override void Update(GameState state)
        {
            _enabled.Update(state);
        }

        public override bool CheckSplit(GameState state)
        {
            return _enabled.CheckSplit(state);
        }

        public enum TBGEntityStateChangeType
        {
            Disabled,
            Enabled
        }
    }
}

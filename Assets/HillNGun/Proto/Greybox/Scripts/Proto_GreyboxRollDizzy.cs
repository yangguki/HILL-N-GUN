using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public enum Proto_GreyboxRollDizzyPhase
    {
        None,
        Light,
        Stun
    }

    public class Proto_GreyboxRollDizzy
    {
        public const float ChainWindowSec = 1.2f;
        public const float LightDurationSec = 1.5f;
        public const float SpinInSec = 2f / 12f;
        public const float SpinBodySec = 1.0f;
        public const float SpinOutSec = 3f / 12f;
        public const float StunDurationSec = SpinInSec + SpinBodySec + SpinOutSec;

        int _chainCount;
        float _lastRollEndTime = -999f;
        Proto_GreyboxRollDizzyPhase _phase = Proto_GreyboxRollDizzyPhase.None;
        float _phaseEndTime;
        float _phaseStartTime;
        int _chainMax = Proto_GreyboxTuning.RollChainMax;
        float _chainLockSec = Proto_GreyboxTuning.RollChainLockSec;
        float _chainLockEndTime = -999f;

        public Proto_GreyboxRollDizzyPhase Phase => _phase;
        public float PhaseElapsed(float time) =>
            _phase == Proto_GreyboxRollDizzyPhase.None ? 0f : UnityEngine.Mathf.Max(0f, time - _phaseStartTime);
        public int ChainCount => _chainCount;
        public int ChainMax => _chainMax;

        public void ApplyRuntime(Proto_GreyboxRuntimeTuning tuning)
        {
            if (tuning == null) return;
            _chainMax = Mathf.Max(1, tuning.RollChainMax);
            _chainLockSec = Mathf.Max(0.1f, tuning.RollChainLockSec);
        }

        public bool ChainLocked(float time) => time < _chainLockEndTime;

        public float ChainLockRemaining(float time) =>
            time < _chainLockEndTime ? UnityEngine.Mathf.Max(0f, _chainLockEndTime - time) : 0f;

        public float ChainLockDuration => _chainLockSec;

        public bool BlocksMovement => _phase == Proto_GreyboxRollDizzyPhase.Stun;
        public bool BlocksFire => _phase == Proto_GreyboxRollDizzyPhase.Stun;
        public bool BlocksRoll => _phase == Proto_GreyboxRollDizzyPhase.Stun;

        public float RemainingTime(float time) =>
            _phase == Proto_GreyboxRollDizzyPhase.None ? 0f : UnityEngine.Mathf.Max(0f, _phaseEndTime - time);

        public void Tick(float time)
        {
            if (_phase != Proto_GreyboxRollDizzyPhase.None && time >= _phaseEndTime)
                _phase = Proto_GreyboxRollDizzyPhase.None;
            // Only reset after a real previous roll ended. Sentinel -999 would
            // otherwise wipe the in-progress first roll's chain count every Tick.
            if (_phase == Proto_GreyboxRollDizzyPhase.None && _lastRollEndTime > -100f
                && time - _lastRollEndTime > ChainWindowSec)
                _chainCount = 0;
        }

        public bool TryAcceptRollStart(float time)
        {
            Tick(time);
            if (BlocksRoll && time < _phaseEndTime)
                return false;
            if (time < _chainLockEndTime)
                return false;
            if (_lastRollEndTime > -100f && time - _lastRollEndTime > ChainWindowSec)
                _chainCount = 0;
            return true;
        }

        public void NotifyRollStarted(float time)
        {
            _chainCount++;
        }

        public void NotifyRollEnded(float time)
        {
            _lastRollEndTime = time;
            if (_chainCount >= _chainMax)
                _chainLockEndTime = time + _chainLockSec;
            if (_chainCount >= 5)
            {
                _phase = Proto_GreyboxRollDizzyPhase.Stun;
                _phaseStartTime = time;
                _phaseEndTime = time + StunDurationSec;
            }
            else if (_chainCount >= 3)
            {
                _phase = Proto_GreyboxRollDizzyPhase.Light;
                _phaseStartTime = time;
                _phaseEndTime = time + LightDurationSec;
            }
        }
    }
}
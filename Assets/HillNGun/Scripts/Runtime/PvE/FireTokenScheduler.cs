using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.PvE
{
    /// <summary>
    /// Fair cap on simultaneously attacking enemies (wind-up + shot). FIFO: a requester is granted only when it is
    /// among the earliest waiting requesters that fit the free slots, so nobody starves. Requesters that stop asking
    /// (out of range, moving) drop out of the queue after <see cref="StaleAfter"/> seconds. <see cref="Reserved"/>
    /// slots are held back for the boss, whose attacks never wait.
    /// </summary>
    public sealed class FireTokenScheduler
    {
        public int Capacity { get; set; }
        public int Reserved { get; set; }
        public float StaleAfter { get; set; } = 0.25f;
        public int ActiveCount => _holders.Count;
        public int WaitingCount => _queue.Count;
        public int PeakActive { get; private set; }
        public int Grants { get; private set; }
        public int Denials { get; private set; }

        private readonly List<object> _holders = new List<object>();
        private readonly List<object> _queue = new List<object>();
        private readonly Dictionary<object, float> _lastAsk = new Dictionary<object, float>();
        private readonly List<object> _tmp = new List<object>();

        public FireTokenScheduler(int capacity) { Capacity = Mathf.Max(1, capacity); }

        public int EnemySlots => Mathf.Max(0, Capacity - Reserved);
        public bool Holds(object who) => _holders.Contains(who);

        public bool TryAcquire(object who, float now)
        {
            if (who == null) return false;
            if (_holders.Contains(who)) return true;
            _lastAsk[who] = now;
            if (!_queue.Contains(who)) _queue.Add(who);
            PruneStale(now);
            int free = EnemySlots - _holders.Count;
            int idx = _queue.IndexOf(who);
            if (free <= 0 || idx < 0 || idx >= free) { Denials++; return false; }
            _queue.RemoveAt(idx);
            _lastAsk.Remove(who);
            _holders.Add(who);
            Grants++;
            if (_holders.Count > PeakActive) PeakActive = _holders.Count;
            return true;
        }

        public void Release(object who)
        {
            if (who == null) return;
            _holders.Remove(who);
            _queue.Remove(who);
            _lastAsk.Remove(who);
        }

        public void Clear()
        {
            _holders.Clear(); _queue.Clear(); _lastAsk.Clear();
        }

        public void ResetStats() { PeakActive = _holders.Count; Grants = 0; Denials = 0; }

        private void PruneStale(float now)
        {
            _tmp.Clear();
            foreach (var q in _queue)
                if (_lastAsk.TryGetValue(q, out float t) && now - t > StaleAfter) _tmp.Add(q);
            foreach (var q in _tmp) { _queue.Remove(q); _lastAsk.Remove(q); }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using HillNGun.Battlefield;

namespace HillNGun.PvE
{
    /// <summary>
    /// FreeMove ally steering around covers (v12). Greedy toward the goal while it makes progress; when the swept move
    /// makes &lt; 35% of the commanded distance for 0.35 s (same band as the [AllyNav] BLOCKED metric) it plans a route on a
    /// 0.4-unit grid over the walk polygon minus live covers (A*, 8-neighbour, inflated by the body radius) and follows
    /// it with sweep-checked shortcuts. Planning only happens on a stall (not per frame). No route (e.g. sealed pocket)
    /// falls back to a 0.6 s side-step toward the freer side. Speed / radius / damage are untouched.
    /// Pure logic so EditMode tests drive it with the real CharacterStateMachine / FreeMoveWorld.
    /// </summary>
    public sealed class AllyFreeSteer
    {
        public const float StallProgress = 0.35f, StallTime = 0.35f, SidestepTime = 0.6f, Probe = 1.2f;
        public const float Cell = 0.4f, RouteLife = 5f, GoalDrift = 1.5f;
        public float stuck, sidestep, sideSign = 1f;
        public int sidesteps, probes, plans, planFails;

        private readonly List<Vector2> _route = new List<Vector2>();
        private int _wp;
        private float _routeAge;
        private Vector2 _routeGoal;

        public bool Sidestepping => sidestep > 0f;
        public bool Routing => _wp < _route.Count;

        public Vector2 Steer(Vector2 goal, Vector2 dir, float dt, bool lastCmdActive, float progress, FreeMoveWorld w, Vector2 pos, float r)
        {
            if (Routing)
            {
                _routeAge += dt;
                if (_routeAge > RouteLife || (goal - _routeGoal).magnitude > GoalDrift) _route.Clear();
                else
                {
                    // shortcut: jump to the furthest waypoint a straight sweep reaches
                    for (int k = _route.Count - 1; k > _wp; k--)
                        if (Reaches(w, pos, _route[k], r)) { _wp = k; break; }
                    if ((_route[_wp] - pos).magnitude < 0.3f) _wp++;
                    if (_wp >= _route.Count) { _route.Clear(); return dir; }
                    if (lastCmdActive && progress < StallProgress) { stuck += dt; if (stuck > StallTime * 2f) { _route.Clear(); stuck = 0f; } }
                    else stuck = 0f;
                    if (Routing) return (_route[_wp] - pos).normalized;
                }
            }
            if (sidestep > 0f)
            {
                sidestep -= dt;
                return (new Vector2(-dir.y, dir.x) * sideSign + dir * 0.25f).normalized;
            }
            if (lastCmdActive && progress < StallProgress)
            {
                stuck += dt;
                if (stuck > StallTime)
                {
                    stuck = 0f;
                    if (w != null && Plan(w, pos, goal, r) && Routing) return (_route[_wp] - pos).normalized;
                    sidestep = SidestepTime; sidesteps++;
                    sideSign = PickSign(w, pos, r, dir, sideSign);
                }
            }
            else stuck = 0f;
            return dir;
        }

        public void Reset() { stuck = 0f; sidestep = 0f; _route.Clear(); _wp = 0; }

        private static bool Reaches(FreeMoveWorld w, Vector2 from, Vector2 to, float r)
            => (w.Move(from, to - from, r) - to).sqrMagnitude < 0.0025f;

        // ---------------------------------------------------------------- grid A*
        private static readonly List<int> _open = new List<int>();
        private static float[] _g; private static int[] _parent; private static byte[] _state; // 0 unknown,1 free,2 blocked
        private static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 }, DY = { 0, 0, 1, -1, 1, -1, 1, -1 };

        private bool Plan(FreeMoveWorld w, Vector2 pos, Vector2 goal, float r)
        {
            plans++;
            var poly = w.Polygon; if (poly == null || poly.Count < 3) { planFails++; return false; }
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < poly.Count; i++) { x0 = Mathf.Min(x0, poly[i].x); y0 = Mathf.Min(y0, poly[i].y); x1 = Mathf.Max(x1, poly[i].x); y1 = Mathf.Max(y1, poly[i].y); }
            int nx = Mathf.Max(2, Mathf.CeilToInt((x1 - x0) / Cell)), ny = Mathf.Max(2, Mathf.CeilToInt((y1 - y0) / Cell));
            int n = nx * ny;
            if (_g == null || _g.Length < n) { _g = new float[n]; _parent = new int[n]; _state = new byte[n]; }
            for (int i = 0; i < n; i++) { _g[i] = float.MaxValue; _parent[i] = -1; _state[i] = 0; }
            float rr = r + 0.02f;
            System.Func<int, Vector2> C = id => new Vector2(x0 + (id % nx + 0.5f) * Cell, y0 + (id / nx + 0.5f) * Cell);
            System.Func<int, bool> Free = id => { if (_state[id] == 0) _state[id] = (byte)(w.IsFree(C(id), rr) ? 1 : 2); return _state[id] == 1; };
            int Clamp(int v, int m) => v < 0 ? 0 : (v >= m ? m - 1 : v);
            int start = Clamp(Mathf.FloorToInt((pos.x - x0) / Cell), nx) + Clamp(Mathf.FloorToInt((pos.y - y0) / Cell), ny) * nx;
            _open.Clear(); _g[start] = 0f; _open.Add(start);
            int best = start; float bestH = (C(start) - goal).magnitude;
            int expanded = 0;
            while (_open.Count > 0 && expanded < 4000)
            {
                int bi = 0; float bf = float.MaxValue;
                for (int i = 0; i < _open.Count; i++) { float f = _g[_open[i]] + (C(_open[i]) - goal).magnitude; if (f < bf) { bf = f; bi = i; } }
                int cur = _open[bi]; _open.RemoveAt(bi); expanded++;
                float h = (C(cur) - goal).magnitude;
                if (h < bestH) { bestH = h; best = cur; }
                if (h < Cell) break;
                int cx = cur % nx, cy = cur / nx;
                for (int k = 0; k < 8; k++)
                {
                    int ax = cx + DX[k], ay = cy + DY[k];
                    if (ax < 0 || ay < 0 || ax >= nx || ay >= ny) continue;
                    int nb = ax + ay * nx;
                    if (!Free(nb)) continue;
                    if (k >= 4 && (!Free(ax + cy * nx) || !Free(cx + ay * nx))) continue; // no corner cutting
                    float g = _g[cur] + (k < 4 ? Cell : Cell * 1.4142f);
                    if (g < _g[nb]) { if (_g[nb] == float.MaxValue) _open.Add(nb); _g[nb] = g; _parent[nb] = cur; }
                }
            }
            if (best == start) { planFails++; return false; }
            _route.Clear();
            for (int id = best; id != -1 && id != start; id = _parent[id]) _route.Add(C(id));
            _route.Reverse();
            if (bestH < Cell * 2f) _route.Add(goal);
            _wp = 0; _routeAge = 0f; _routeGoal = goal;
            return true;
        }

        private float PickSign(FreeMoveWorld w, Vector2 pos, float r, Vector2 dir, float lastSign)
        {
            if (w == null) return -lastSign;
            probes++;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float a = (w.Move(pos, (perp + dir * 0.25f).normalized * Probe, r) - pos).magnitude;
            float b = (w.Move(pos, (-perp + dir * 0.25f).normalized * Probe, r) - pos).magnitude;
            if (Mathf.Max(a, b) < 0.3f) return -lastSign;      // pocket: both sides shut -> alternate
            if (Mathf.Abs(a - b) < 0.1f) return lastSign;      // open both ways: keep going the same way (no ping-pong)
            return a > b ? 1f : -1f;
        }
    }
}

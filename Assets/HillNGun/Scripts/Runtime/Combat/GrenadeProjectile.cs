using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Combat
{
    public class GrenadeProjectile
    {
        public Vector2 StartPosition { get; private set; }
        public Vector2 TargetPosition { get; private set; }
        public Vector2 CurrentPosition { get; private set; }
        public float FlightTime { get; private set; }
        public float ElapsedTime { get; private set; }
        public Team OwnerTeam { get; private set; }
        public GrenadeData Data { get; private set; }
        public bool IsActive { get; private set; }
        public float HeightOffset { get; private set; }

        /// <summary>Arc position used by the view (ground lerp + parabola height).</summary>
        public Vector2 VisualPosition => CurrentPosition + new Vector2(0f, HeightOffset);

        /// <summary>0..1 flight progress.</summary>
        public float Progress => FlightTime > 0f ? Mathf.Clamp01(ElapsedTime / FlightTime) : 1f;

        public void Initialize(Vector2 start, Vector2 target, GrenadeData data, Team team)
        {
            StartPosition = start;
            TargetPosition = target;
            CurrentPosition = start;
            Data = data;
            OwnerTeam = team;
            
            float distance = Vector2.Distance(start, target);
            FlightTime = data.GetFlightTime(distance);
            ElapsedTime = 0f;
            IsActive = true;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            ElapsedTime += deltaTime;
            float t = Mathf.Clamp01(ElapsedTime / FlightTime);

            CurrentPosition = Vector2.Lerp(StartPosition, TargetPosition, t);

            float parabola = 4f * t * (1f - t);
            float maxHeight = Vector2.Distance(StartPosition, TargetPosition) * 0.3f;
            HeightOffset = parabola * maxHeight;
        }

        public bool HasLanded()
        {
            return ElapsedTime >= FlightTime;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Reset()
        {
            IsActive = false;
            ElapsedTime = 0f;
        }
    }
}

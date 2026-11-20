using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxScreenMetrics
    {
        public static void ConfigureCaptureCamera(Camera cam)
        {
            cam.orthographicSize = Proto_GreyboxLayout.CameraOrthographicSize;
            cam.aspect = Proto_GreyboxLayout.TargetAspect;
        }

        public static void FromBoundsViaCamera(Camera cam, Bounds bounds, out float topPercent, out float bottomPercent,
            out float heightPercent, out float leftPercent, out float rightPercent, out float centerXPercent)
        {
            float z = bounds.center.z;
            var bl = cam.WorldToViewportPoint(new Vector3(bounds.min.x, bounds.min.y, z));
            var br = cam.WorldToViewportPoint(new Vector3(bounds.max.x, bounds.min.y, z));
            var tl = cam.WorldToViewportPoint(new Vector3(bounds.min.x, bounds.max.y, z));
            var tr = cam.WorldToViewportPoint(new Vector3(bounds.max.x, bounds.max.y, z));

            float minVx = Mathf.Min(Mathf.Min(bl.x, br.x), Mathf.Min(tl.x, tr.x));
            float maxVx = Mathf.Max(Mathf.Max(bl.x, br.x), Mathf.Max(tl.x, tr.x));
            float minVy = Mathf.Min(Mathf.Min(bl.y, br.y), Mathf.Min(tl.y, tr.y));
            float maxVy = Mathf.Max(Mathf.Max(bl.y, br.y), Mathf.Max(tl.y, tr.y));

            leftPercent = minVx * 100f;
            rightPercent = maxVx * 100f;
            centerXPercent = (minVx + maxVx) * 0.5f * 100f;
            topPercent = (1f - maxVy) * 100f;
            bottomPercent = (1f - minVy) * 100f;
            heightPercent = bottomPercent - topPercent;
        }

        public static void FromBounds(Bounds bounds, out float topPercent, out float bottomPercent,
            out float heightPercent, out float leftPercent, out float rightPercent, out float centerXPercent)
        {
            topPercent = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(bounds.max.y);
            bottomPercent = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(bounds.min.y);
            heightPercent = Proto_GreyboxLayout.WorldHeightToScreenPercent(bounds.size.y);
            leftPercent = (bounds.min.x + Proto_GreyboxLayout.WorldWidth * 0.5f) / Proto_GreyboxLayout.WorldWidth * 100f;
            rightPercent = (bounds.max.x + Proto_GreyboxLayout.WorldWidth * 0.5f) / Proto_GreyboxLayout.WorldWidth * 100f;
            centerXPercent = (bounds.center.x + Proto_GreyboxLayout.WorldWidth * 0.5f) / Proto_GreyboxLayout.WorldWidth * 100f;
        }

        public static Renderer FindRenderer(string objectName)
        {
            var go = GameObject.Find(objectName);
            return go != null ? go.GetComponent<Renderer>() : null;
        }
    }
}
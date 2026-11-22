using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>
    /// Keeps dress primitives (hat, hair, bandana, holster) locked to a bone
    /// without inheriting the bone's animated scale, so they don't drift.
    /// Upright pieces follow the character yaw so the hat brim stays level.
    /// </summary>
    public sealed class Proto_Greybox3DDressFollow : MonoBehaviour
    {
        Transform _bone;
        Transform _body;
        Vector3 _up, _fwd, _right;
        Vector3 _worldScale;
        bool _upright;
        Quaternion _boneLocalRot = Quaternion.identity;

        public Transform Bone => _bone;

        public static Transform Attach(Transform bone, Transform body, string name,
            float up, float fwd, float right, Vector3 worldScale, bool upright)
        {
            var t = new GameObject(name).transform;
            t.SetParent(body, false);
            var follow = t.gameObject.AddComponent<Proto_Greybox3DDressFollow>();
            follow._bone = bone;
            follow._body = body != null ? body : bone;
            follow._up = Vector3.up * up;
            follow._fwd = Vector3.forward * fwd;
            follow._right = Vector3.right * right;
            follow._worldScale = worldScale;
            follow._upright = upright;
            follow.Apply();
            return t;
        }

        public static Transform AttachLocal(Transform bone, string name, Vector3 localPos, Quaternion localRot,
            Vector3 worldScale)
        {
            var t = new GameObject(name).transform;
            t.SetParent(bone, false);
            t.localPosition = localPos;
            t.localRotation = localRot;
            var follow = t.gameObject.AddComponent<Proto_Greybox3DDressFollow>();
            follow._bone = bone;
            follow._body = bone;
            follow._worldScale = worldScale;
            follow._upright = false;
            follow._boneLocalRot = localRot;
            follow.ApplyScaleOnly();
            return t;
        }

        void LateUpdate() => Apply();

        public void Apply()
        {
            if (_bone == null) return;
            if (_upright)
            {
                var body = _body != null ? _body : _bone;
                var pos = _bone.position + body.TransformVector(_up + _fwd + _right);
                transform.position = pos;
                var fwd = Vector3.ProjectOnPlane(body.forward, Vector3.up);
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
                transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
            }
            ApplyScaleOnly();
        }

        void ApplyScaleOnly()
        {
            if (_worldScale.sqrMagnitude < 1e-8f) return;
            var ls = transform.lossyScale;
            transform.localScale = new Vector3(
                _worldScale.x / Mathf.Max(1e-5f, ls.x / Mathf.Max(1e-5f, transform.localScale.x)),
                _worldScale.y / Mathf.Max(1e-5f, ls.y / Mathf.Max(1e-5f, transform.localScale.y)),
                _worldScale.z / Mathf.Max(1e-5f, ls.z / Mathf.Max(1e-5f, transform.localScale.z)));
        }
    }
}

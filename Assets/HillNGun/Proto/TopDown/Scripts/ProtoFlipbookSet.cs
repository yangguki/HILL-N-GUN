using System;
using UnityEngine;

namespace HillNGun.Proto.TopDown
{
    /// <summary>Per-character idle/run/shoot flipbooks keyed by facing. Built from stage1 meta JSON.</summary>
    [CreateAssetMenu(menuName = "HillNGun/Proto/Flipbook Set", fileName = "ProtoFlipbookSet")]
    public class ProtoFlipbookSet : ScriptableObject
    {
        [Serializable]
        public class Clip
        {
            public ProtoFacingDir facing;
            public ProtoAnimKind anim;
            public Sprite[] frames;
            public float fps = 12f;
            public bool loop = true;
            public bool flipXForLeft;
            public Vector2 pivotPx = new Vector2(64f, 124f);

            public bool Ready => frames != null && frames.Length > 0 && frames[0] != null;
            public int FrameCount => frames != null ? frames.Length : 0;
        }

        public string characterId;
        public int stage = 1;
        public Clip[] clips;

        public Clip Find(ProtoFacingDir facing, ProtoAnimKind anim)
        {
            if (clips == null) return null;
            for (int i = 0; i < clips.Length; i++)
            {
                var c = clips[i];
                if (c != null && c.facing == facing && c.anim == anim && c.Ready) return c;
            }
            return null;
        }

        public bool TryGet(ProtoFacingDir facing, ProtoAnimKind anim, out Clip clip)
        {
            clip = Find(facing, anim);
            return clip != null;
        }
    }
}

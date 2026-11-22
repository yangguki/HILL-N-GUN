using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DSpawn
    {
        public static Transform Prefab(GameObject prefab, Transform parent, string name, Vector3 pos,
            Vector3 euler, float uniformScale)
        {
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            go.transform.localScale = Vector3.one * uniformScale;
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                Proto_Greybox3DUtil.DestroySafe(c);
            return go.transform;
        }

        public static void Tint(Transform t, Color color, float outline = 0.012f)
        {
            if (t == null) return;
            var mat = Proto_Greybox3DMaterials.Cel(color, outline: outline);
            foreach (var r in t.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;
        }

        public static Transform PrefabOrCube(GameObject prefab, Transform parent, string name, Vector3 pos,
            Vector3 euler, float uniformScale, Vector3 cubeScale, Material mat)
        {
            var t = Prefab(prefab, parent, name, pos, euler, uniformScale);
            if (t != null) return t;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = pos;
            cube.transform.rotation = Quaternion.Euler(euler);
            cube.transform.localScale = cubeScale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return cube.transform;
        }
    }
}

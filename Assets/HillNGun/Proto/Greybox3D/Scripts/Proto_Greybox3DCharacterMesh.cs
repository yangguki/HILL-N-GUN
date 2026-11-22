using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DCharacterMesh
    {
        public static Transform BuildHero(Transform parent)
        {
            var root = new GameObject("Greybox3D_Hero").transform;
            root.SetParent(parent, false);

            // boots / pants
            AddPart(root, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.40f, 0.36f, 0.32f),
                new Color(0.15f, 0.32f, 0.72f));
            // purple shirt
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.52f, 0f), new Vector3(0.44f, 0.28f, 0.38f),
                new Color(0.48f, 0.20f, 0.68f));
            // head (under hat)
            AddPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.92f, 0.02f), new Vector3(0.36f, 0.36f, 0.34f),
                new Color(0.92f, 0.78f, 0.62f));
            // red bandana (front + back wrap)
            AddPart(root, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.14f), new Vector3(0.28f, 0.07f, 0.06f),
                new Color(0.85f, 0.15f, 0.12f));
            AddPart(root, PrimitiveType.Cube, new Vector3(0f, 0.76f, -0.14f), new Vector3(0.16f, 0.10f, 0.06f),
                new Color(0.75f, 0.10f, 0.10f));
            // big purple hat: crown on top, wide brim
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, 1.12f, 0f), new Vector3(0.42f, 0.16f, 0.42f),
                new Color(0.42f, 0.16f, 0.66f));
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, 1.00f, 0f), new Vector3(0.85f, 0.035f, 0.85f),
                new Color(0.38f, 0.14f, 0.62f));
            // revolver
            AddPart(root, PrimitiveType.Cube, new Vector3(0.28f, 0.42f, 0.16f), new Vector3(0.08f, 0.05f, 0.28f),
                new Color(0.18f, 0.18f, 0.20f));

            return root;
        }

        public static Transform BuildGrunt(Transform parent)
        {
            var root = new GameObject("Greybox3D_Grunt").transform;
            root.SetParent(parent, false);

            AddPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.9f, 0f), new Vector3(0.46f, 0.46f, 0.42f),
                new Color(0.92f, 0.78f, 0.62f));
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.48f, 0f), new Vector3(0.36f, 0.28f, 0.34f),
                new Color(0.55f, 0.4f, 0.28f));
            AddPart(root, PrimitiveType.Cube, new Vector3(0f, 1.02f, 0f), new Vector3(0.5f, 0.1f, 0.42f),
                new Color(0.42f, 0.3f, 0.18f));
            AddPart(root, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.1f), new Vector3(0.28f, 0.06f, 0.06f),
                new Color(0.85f, 0.15f, 0.12f));
            AddPart(root, PrimitiveType.Cube, new Vector3(0.18f, 0.52f, 0.06f), new Vector3(0.06f, 0.04f, 0.18f),
                new Color(0.2f, 0.2f, 0.22f));

            return root;
        }

        static void AddPart(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null) Proto_Greybox3DUtil.DestroySafe(col);
            go.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.Cel(color);
        }
    }
}
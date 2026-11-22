using HillNGun.Proto.Greybox;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DSceneBuilder
    {
        static readonly Color[] AwningColors =
        {
            new Color(0.2f, 0.45f, 0.85f),
            new Color(0.95f, 0.82f, 0.2f),
            new Color(0.85f, 0.22f, 0.18f),
            new Color(0.25f, 0.7f, 0.45f),
            new Color(0.7f, 0.35f, 0.75f)
        };

        public static Transform BuildRoot(Transform parent)
        {
            var root = new GameObject("Greybox3D_Stage").transform;
            root.SetParent(parent, false);

            BuildSky(root);
            BuildGround(root);
            BuildMesas(root);
            BuildStorefronts(root);
            BuildFence(root);
            BuildCrates(root);
            BuildCacti(root);
            EnsureLight(root);
            return root;
        }

        static void BuildSky(Transform root)
        {
            var sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sky.name = "Greybox3D_Sky";
            sky.transform.SetParent(root, false);
            sky.transform.localScale = Vector3.one * 120f;
            sky.transform.position = new Vector3(0f, 25f, 35f);
            var col = sky.GetComponent<Collider>();
            if (col != null) Proto_Greybox3DUtil.DestroySafe(col);
            sky.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedSky;
        }

        static void BuildGround(Transform root)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Greybox3D_Ground";
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(0f, -0.15f, 11f);
            ground.transform.localScale = new Vector3(Proto_GreyboxLayout.WorldWidth * 1.1f, 0.3f, 36f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedGround;
        }

        static void BuildMesas(Transform root)
        {
            for (int i = 0; i < 4; i++)
            {
                var mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mesa.name = "Greybox3D_Mesa_" + i;
                mesa.transform.SetParent(root, false);
                float x = Mathf.Lerp(-8f, 8f, i / 3f);
                mesa.transform.position = new Vector3(x, 2.5f, 30f + i * 1.5f);
                mesa.transform.localScale = new Vector3(6f + i, 3f + i * 0.4f, 4f);
                mesa.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.Cel(new Color(0.72f, 0.5f, 0.35f));
            }
        }

        static void BuildStorefronts(Transform root)
        {
            int blocks = 5;
            float span = Proto_GreyboxLayout.WorldWidth;
            float blockW = span / blocks;
            float z = Proto_Greybox3DFootMap.FootWorldYToDepthZ(
                Proto_GreyboxLayout.ScreenYPercentToWorldY(32f)) + 2f;

            for (int i = 0; i < blocks; i++)
            {
                float cx = -span * 0.5f + blockW * (i + 0.5f);
                var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
                face.name = "Greybox3D_Store_" + i;
                face.transform.SetParent(root, false);
                face.transform.position = new Vector3(cx, 2.2f, z);
                face.transform.localScale = new Vector3(blockW * 0.88f, 4.5f, 1.6f);
                face.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.Cel(new Color(0.78f, 0.72f, 0.62f));

                var awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
                awning.name = "Greybox3D_Awning_" + i;
                awning.transform.SetParent(face.transform, false);
                awning.transform.localPosition = new Vector3(0f, 0.55f, -0.62f);
                awning.transform.localScale = new Vector3(1.02f, 0.10f, 0.42f);
                awning.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.Cel(AwningColors[i % AwningColors.Length]);

                // two windows + a door so the block reads as a storefront
                var winL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                winL.name = "Greybox3D_WindowL_" + i;
                winL.transform.SetParent(face.transform, false);
                winL.transform.localPosition = new Vector3(-0.22f, 0.18f, -0.51f);
                winL.transform.localScale = new Vector3(0.22f, 0.22f, 0.04f);
                winL.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.Cel(new Color(0.25f, 0.28f, 0.38f));

                var winR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                winR.name = "Greybox3D_WindowR_" + i;
                winR.transform.SetParent(face.transform, false);
                winR.transform.localPosition = new Vector3(0.22f, 0.18f, -0.51f);
                winR.transform.localScale = new Vector3(0.22f, 0.22f, 0.04f);
                winR.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.Cel(new Color(0.25f, 0.28f, 0.38f));

                var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.name = "Greybox3D_Door_" + i;
                door.transform.SetParent(face.transform, false);
                door.transform.localPosition = new Vector3(0f, -0.22f, -0.51f);
                door.transform.localScale = new Vector3(0.18f, 0.48f, 0.04f);
                door.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedWood;
            }
        }

        static void BuildFence(Transform root)
        {
            float z = Proto_Greybox3DFootMap.FenceDepthZ;
            float gapHalf = Proto_GreyboxLayout.WorldWidth * 0.08f;
            float halfW = Proto_GreyboxLayout.WorldWidth * 0.5f;

            void Rail(float x0, float x1)
            {
                float cx = (x0 + x1) * 0.5f;
                float w = x1 - x0;
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.transform.SetParent(root, false);
                rail.transform.position = new Vector3(cx, 0.65f, z);
                rail.transform.localScale = new Vector3(w, 0.9f, 0.14f);
                rail.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedFence;
            }

            Rail(-halfW, -gapHalf);
            Rail(gapHalf, halfW);

            void Post(float x)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.transform.SetParent(root, false);
                post.transform.position = new Vector3(x, 0.7f, z);
                post.transform.localScale = new Vector3(0.12f, 1.4f, 0.12f);
                post.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedFence;
            }

            Post(-gapHalf);
            Post(gapHalf);
            Post(-halfW + 0.2f);
            Post(halfW - 0.2f);
        }

        static void BuildCrates(Transform root)
        {
            SpawnCrate(root, Proto_GreyboxLayout.ScreenXPercentToWorldX(54f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(67f), 0.9f);
            SpawnCrate(root, Proto_GreyboxLayout.ScreenXPercentToWorldX(16f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(58f), 0.75f);
            SpawnCrate(root, Proto_GreyboxLayout.ScreenXPercentToWorldX(63f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(72f), 1f);
        }

        static void SpawnCrate(Transform root, float footX, float footY, float scale)
        {
            var pos = Proto_Greybox3DFootMap.FootToWorld(new Vector2(footX, footY));
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Greybox3D_Crate";
            crate.transform.SetParent(root, false);
            crate.transform.position = pos + new Vector3(0f, 0.35f * scale, 0f);
            crate.transform.localScale = Vector3.one * (0.7f * scale);
            crate.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedWood;
        }

        static void BuildCacti(Transform root)
        {
            float[] xs = { -3.5f, -1.2f, 2.4f, 4.8f };
            float z = Proto_Greybox3DFootMap.FenceDepthZ - 1.2f;
            foreach (var x in xs)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.name = "Greybox3D_Cactus";
                body.transform.SetParent(root, false);
                body.transform.position = new Vector3(x, 0.55f, z);
                body.transform.localScale = new Vector3(0.22f, 0.55f, 0.22f);
                body.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.Cel(new Color(0.25f, 0.62f, 0.28f));
            }
        }

        static void EnsureLight(Transform root)
        {
            var lightGo = new GameObject("Greybox3D_Sun");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.rotation = Quaternion.Euler(48f, -25f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.None;
        }
    }
}
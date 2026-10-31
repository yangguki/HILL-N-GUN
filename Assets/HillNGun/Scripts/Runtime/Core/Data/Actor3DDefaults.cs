using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>Default 3D model definitions matching the deck character designs (colours / silhouettes only, all original).</summary>
    public static class Actor3DDefaults
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        public static Actor3DModelDef[] Models() => new[] { Buster(), Raven(), Volt(), PistolMook(), DynamiteBandit(), MountedRaider(), Rex() };

        public static Actor3DModelDef Buster() => new Actor3DModelDef
        {
            id = "buster", bulk = 1.25f, shoulderWidth = 0.58f, torsoHeight = 0.54f, limbRadius = 0.085f,
            skin = C(0.88f, 0.64f, 0.46f), hair = C(0.30f, 0.18f, 0.10f),
            hatColor = C(0.55f, 0.34f, 0.17f), hatBand = C(0.80f, 0.16f, 0.12f),
            shirt = C(0.80f, 0.72f, 0.58f), vest = C(0.47f, 0.27f, 0.13f), pants = C(0.30f, 0.33f, 0.42f),
            boots = C(0.33f, 0.19f, 0.10f), belt = C(0.22f, 0.13f, 0.07f), accent = C(0.84f, 0.18f, 0.13f),
            metal = C(0.36f, 0.38f, 0.42f), wood = C(0.55f, 0.33f, 0.15f),
            hat = Actor3DHat.Cowboy, hairStyle = Actor3DHair.Short, face = Actor3DFace.None, neckerchief = true, bareArms = true,
            weapon = Actor3DWeapon.Shotgun, twoHanded = true, hasVest = true, mustache = false,
        };

        public static Actor3DModelDef Raven() => new Actor3DModelDef
        {
            id = "raven", bulk = 0.9f, shoulderWidth = 0.46f, hipWidth = 0.32f, legLength = 0.66f,
            skin = C(0.96f, 0.82f, 0.72f), hair = C(0.10f, 0.08f, 0.13f),
            hatColor = C(0.40f, 0.22f, 0.52f), hatBand = C(0.20f, 0.12f, 0.28f),
            shirt = C(0.17f, 0.15f, 0.22f), vest = C(0.24f, 0.18f, 0.30f), pants = C(0.15f, 0.13f, 0.19f),
            boots = C(0.12f, 0.10f, 0.12f), belt = C(0.30f, 0.20f, 0.32f), accent = C(0.62f, 0.40f, 0.78f),
            coat = C(0.30f, 0.17f, 0.40f), metal = C(0.30f, 0.32f, 0.36f), wood = C(0.36f, 0.22f, 0.14f),
            hat = Actor3DHat.WitchWide, hairStyle = Actor3DHair.Long, face = Actor3DFace.None, cape = true, hasVest = false,
            weapon = Actor3DWeapon.Rifle, twoHanded = true,
        };

        public static Actor3DModelDef Volt() => new Actor3DModelDef
        {
            id = "volt", bulk = 0.95f, shoulderWidth = 0.48f, legLength = 0.58f,
            skin = C(0.93f, 0.74f, 0.58f), hair = C(0.48f, 0.28f, 0.14f),
            hatColor = C(0.50f, 0.33f, 0.18f), hatBand = C(0.30f, 0.20f, 0.10f),
            shirt = C(0.90f, 0.84f, 0.70f), vest = C(0.24f, 0.44f, 0.74f), pants = C(0.24f, 0.42f, 0.72f),
            boots = C(0.36f, 0.22f, 0.12f), belt = C(0.26f, 0.16f, 0.08f), accent = C(0.95f, 0.78f, 0.25f),
            metal = C(0.62f, 0.64f, 0.68f), wood = C(0.45f, 0.30f, 0.18f),
            hat = Actor3DHat.Cowboy, hairStyle = Actor3DHair.Curly, face = Actor3DFace.None, overalls = true, hasVest = false,
            wrenchOnBack = true, weapon = Actor3DWeapon.Pistol,
        };

        public static Actor3DModelDef PistolMook() => new Actor3DModelDef
        {
            id = "pistol_mook", bulk = 1f,
            skin = C(0.86f, 0.64f, 0.48f), hair = C(0.25f, 0.16f, 0.10f),
            hatColor = C(0.42f, 0.27f, 0.15f), hatBand = C(0.74f, 0.14f, 0.11f),
            shirt = C(0.50f, 0.57f, 0.66f), vest = C(0.44f, 0.28f, 0.15f), pants = C(0.26f, 0.25f, 0.29f),
            boots = C(0.25f, 0.15f, 0.08f), accent = C(0.78f, 0.15f, 0.12f), metal = C(0.40f, 0.42f, 0.46f),
            hat = Actor3DHat.Cowboy, face = Actor3DFace.Bandana, weapon = Actor3DWeapon.Pistol, hasVest = true,
        };

        public static Actor3DModelDef DynamiteBandit() => new Actor3DModelDef
        {
            id = "dynamite_bandit", bulk = 1.1f, shoulderWidth = 0.54f, bodyDepth = 0.34f,
            skin = C(0.88f, 0.66f, 0.50f), hair = C(0.16f, 0.12f, 0.10f),
            hatColor = C(0.13f, 0.13f, 0.14f), hatBand = C(0.30f, 0.30f, 0.32f),
            shirt = C(0.46f, 0.50f, 0.26f), vest = C(0.33f, 0.36f, 0.18f), pants = C(0.46f, 0.31f, 0.18f),
            boots = C(0.24f, 0.15f, 0.08f), belt = C(0.26f, 0.16f, 0.08f), accent = C(0.82f, 0.20f, 0.14f),
            hat = Actor3DHat.Bowler, face = Actor3DFace.None, mustache = true, stripedShirt = true, bandolier = true, hasVest = false,
            weapon = Actor3DWeapon.Dynamite,
        };

        public static Actor3DModelDef MountedRaider() => new Actor3DModelDef
        {
            id = "mounted_raider", bulk = 1f,
            skin = C(0.84f, 0.62f, 0.46f), hair = C(0.22f, 0.14f, 0.08f),
            hatColor = C(0.78f, 0.62f, 0.40f), hatBand = C(0.40f, 0.26f, 0.14f),
            shirt = C(0.36f, 0.31f, 0.28f), vest = C(0.92f, 0.52f, 0.18f), pants = C(0.30f, 0.26f, 0.24f),
            boots = C(0.28f, 0.17f, 0.09f), accent = C(0.16f, 0.18f, 0.27f), metal = C(0.36f, 0.38f, 0.42f), wood = C(0.50f, 0.30f, 0.14f),
            hat = Actor3DHat.Wide, face = Actor3DFace.Mask, weapon = Actor3DWeapon.Rifle, twoHanded = true, hasVest = true,
            mounted = true, horseColor = C(0.48f, 0.29f, 0.15f), horseMane = C(0.17f, 0.10f, 0.06f),
        };

        public static Actor3DModelDef Rex() => new Actor3DModelDef
        {
            id = "rattlesnake_rex", bulk = 1.15f, shoulderWidth = 0.56f, legLength = 0.66f, headRadius = 0.31f,
            skin = C(0.45f, 0.62f, 0.25f), hair = C(0.30f, 0.42f, 0.16f), eyes = C(0.98f, 0.82f, 0.15f),
            hatColor = C(0.76f, 0.60f, 0.38f), hatBand = C(0.30f, 0.20f, 0.12f),
            shirt = C(0.86f, 0.80f, 0.62f), vest = C(0.42f, 0.26f, 0.14f), pants = C(0.40f, 0.42f, 0.22f),
            boots = C(0.36f, 0.22f, 0.11f), belt = C(0.25f, 0.15f, 0.07f), accent = C(0.80f, 0.20f, 0.14f),
            coat = C(0.74f, 0.62f, 0.44f), metal = C(0.55f, 0.56f, 0.60f), wood = C(0.40f, 0.25f, 0.12f),
            hat = Actor3DHat.Cowboy, hairStyle = Actor3DHair.None, face = Actor3DFace.Snake, longCoat = true, hasVest = true,
            weapon = Actor3DWeapon.DualPistol, tail = true, tailSegments = 8, tailLength = 1.5f,
        };
    }
}

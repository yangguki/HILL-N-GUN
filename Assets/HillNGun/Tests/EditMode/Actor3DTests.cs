using NUnit.Framework;
using HillNGun.Data;
using HillNGun.View3D;
using UnityEngine;

namespace HillNGun.Tests.EditMode
{
    /// <summary>3D actor view data sanity: every playable/enemy/boss id has a model; mesh kit builds closed, outward-facing geometry.</summary>
    [TestFixture]
    public class Actor3DTests
    {
        [Test]
        public void DefaultModels_CoverAllActors()
        {
            var models = Actor3DDefaults.Models();
            foreach (var id in new[] { "buster", "raven", "volt", "pistol_mook", "dynamite_bandit", "mounted_raider", "rattlesnake_rex" })
                Assert.IsTrue(System.Array.Exists(models, m => m.id == id), "missing 3D model for " + id);
        }

        [Test]
        public void MeshBuilder_SphereFacesOutward_AndHasOutlineNormals()
        {
            var b = new ToonMeshBuilder();
            b.Sphere(Vector3.zero, 1f, Color.white);
            var mesh = b.Build("t");
            var v = mesh.vertices; var t = mesh.triangles;
            int outward = 0, total = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], c1 = v[t[i + 1]], c2 = v[t[i + 2]];
                var n = Vector3.Cross(c1 - a, c2 - a);
                if (n.sqrMagnitude < 1e-10f) continue;
                total++;
                if (Vector3.Dot(n, (a + c1 + c2) / 3f) > 0f) outward++;
            }
            // Unity: clockwise winding = front face -> cross product points outward
            Assert.Greater(outward, total * 0.95f);
            Assert.AreEqual(v.Length, mesh.tangents.Length);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void AnimData_RollTuckEndsUntucked()
        {
            var a = ScriptableObject.CreateInstance<Actor3DAnimData>();
            Assert.AreEqual(0f, a.RollTuck(0f), 0.01f);
            Assert.AreEqual(0f, a.RollTuck(1f), 0.01f);
            Assert.Greater(a.RollTuck(0.5f), 0.8f);
            Object.DestroyImmediate(a);
        }
    }
}

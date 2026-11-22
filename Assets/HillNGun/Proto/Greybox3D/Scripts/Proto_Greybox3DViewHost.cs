using HillNGun.Proto.Greybox;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public sealed class Proto_Greybox3DViewHost : IProto_GreyboxViewPresenter
    {
        Proto_Greybox3DViewPresenter _presenter;

        public Camera WorldCamera => PresenterAlive ? _presenter.WorldCamera : null;

        // UnityEngine.Object overload: a destroyed presenter is not a C# null,
        // but compares equal to null. Do not use `?.` / `is null` on it.
        bool PresenterAlive => _presenter != null;

        public void EnsureCreated()
        {
            if (PresenterAlive) return;
            _presenter = null;
            GameObject go = null;
            try
            {
                DestroyOrphanViewRoots();
                Proto_Greybox3DMaterials.BindFromCatalog(Proto_Greybox3DCatalog.Load());
                // Keep ViewRoot at the scene root so StageBuilder.ClearRoot
                // (FullRestart → Build) cannot wipe the 3D stage.
                go = new GameObject("Greybox3D_ViewRoot");
                var presenter = go.AddComponent<Proto_Greybox3DViewPresenter>();
                presenter.EnsureCreated();
                _presenter = presenter;
            }
            catch (System.Exception e)
            {
                _presenter = null;
                if (go != null)
                    Proto_Greybox3DUtil.DestroySafe(go);
                Proto_Greybox3DFault.Report("EnsureCreated", e);
                throw;
            }
        }

        public void SetActive(bool active, Proto_GreyboxPlayerController host)
        {
            try
            {
                if (active)
                    EnsureCreated();
                if (!PresenterAlive)
                    return;
                _presenter.SetActive(active, host);
                if (active)
                    Proto_Greybox3DFault.Clear();
            }
            catch (System.Exception e)
            {
                Proto_Greybox3DFault.Report("SetActive", e);
                throw;
            }
        }

        public void Sync(Proto_GreyboxPlayerController host)
        {
            try
            {
                if (!PresenterAlive)
                {
                    _presenter = null;
                    // Rebuild if the 3D view was requested but the root was
                    // destroyed (stage restart used Destroy(), deferred).
                    if (host != null && Proto_GreyboxViewModeState.Is3D)
                    {
                        EnsureCreated();
                        if (PresenterAlive)
                        {
                            _presenter.SetActive(true, host);
                            Proto_Greybox3DFault.Clear();
                            return;
                        }
                    }
                }
                if (!PresenterAlive)
                    return;
                _presenter.Sync(host);
            }
            catch (System.Exception e)
            {
                Proto_Greybox3DFault.Report("Sync", e);
                throw;
            }
        }

        static void DestroyOrphanViewRoots()
        {
            // Find only returns one; rename so the next search can see another.
            for (int i = 0; i < 8; i++)
            {
                var go = GameObject.Find("Greybox3D_ViewRoot");
                if (go == null) break;
                go.name = "Greybox3D_ViewRoot_Dead";
                Proto_Greybox3DUtil.DestroySafe(go);
            }
        }
    }
}

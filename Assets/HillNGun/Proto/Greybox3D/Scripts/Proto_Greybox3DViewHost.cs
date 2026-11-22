using HillNGun.Proto.Greybox;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public sealed class Proto_Greybox3DViewHost : IProto_GreyboxViewPresenter
    {
        Proto_Greybox3DViewPresenter _presenter;

        public Camera WorldCamera => _presenter != null ? _presenter.WorldCamera : null;

        public void EnsureCreated()
        {
            if (_presenter != null) return;
            GameObject go = null;
            try
            {
                Proto_Greybox3DMaterials.BindFromCatalog(Proto_Greybox3DCatalog.Load());
                var parent = UnityEngine.Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>()?.transform;
                go = new GameObject("Greybox3D_ViewRoot");
                if (parent != null)
                    go.transform.SetParent(parent, false);
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
                _presenter?.SetActive(active, host);
            }
            catch (System.Exception e)
            {
                Proto_Greybox3DFault.Report("SetActive", e);
            }
        }

        public void Sync(Proto_GreyboxPlayerController host)
        {
            try
            {
                _presenter?.Sync(host);
            }
            catch (System.Exception e)
            {
                Proto_Greybox3DFault.Report("Sync", e);
            }
        }
    }
}

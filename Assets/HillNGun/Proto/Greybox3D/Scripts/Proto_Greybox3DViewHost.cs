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
            var parent = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>()?.transform;
            var go = new GameObject("Greybox3D_ViewRoot");
            if (parent != null)
                go.transform.SetParent(parent, false);
            _presenter = go.AddComponent<Proto_Greybox3DViewPresenter>();
            _presenter.EnsureCreated();
        }

        public void SetActive(bool active, Proto_GreyboxPlayerController host) =>
            _presenter.SetActive(active, host);

        public void Sync(Proto_GreyboxPlayerController host) => _presenter.Sync(host);
    }
}
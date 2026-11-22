namespace HillNGun.Proto.Greybox
{
    public interface IProto_GreyboxViewPresenter
    {
        void EnsureCreated();
        void SetActive(bool active, Proto_GreyboxPlayerController host);
        void Sync(Proto_GreyboxPlayerController host);
        UnityEngine.Camera WorldCamera { get; }
    }
}
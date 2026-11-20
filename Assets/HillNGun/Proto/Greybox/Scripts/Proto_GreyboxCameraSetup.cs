using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class Proto_GreyboxCameraSetup : MonoBehaviour
    {
        void OnEnable() => Apply();
        void Update()
        {
            if (!Application.isPlaying)
                Apply();
        }

        public void Apply()
        {
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = Proto_GreyboxLayout.CameraOrthographicSize;
            cam.aspect = Proto_GreyboxLayout.TargetAspect;
            transform.position = new Vector3(0f, Proto_GreyboxLayout.CameraPositionY, Proto_GreyboxLayout.CameraPositionZ);
            transform.rotation = Quaternion.identity;
        }
    }
}
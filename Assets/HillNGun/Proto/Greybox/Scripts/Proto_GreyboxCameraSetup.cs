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
            cam.aspect = Application.isPlaying && Screen.height > 0
                ? (float)Screen.width / Screen.height
                : Proto_GreyboxLayout.TargetAspect;
            transform.position = new Vector3(0f, Proto_GreyboxLayout.CameraPositionY, Proto_GreyboxLayout.CameraPositionZ);
            transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Proto_GreyboxViewModeState.IsPersp2D
                ? new Color32(0x3c, 0x2a, 0x58, 0xff)
                : new Color32(38, 38, 46, 255);
        }
    }
}
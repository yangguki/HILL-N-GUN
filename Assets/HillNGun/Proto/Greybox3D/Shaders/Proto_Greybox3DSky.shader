Shader "HillNGun/Proto/Greybox3D/Sky"
{
    Properties
    {
        _Color ("Zenith", Color) = (0.04, 0.26, 0.90, 1)
        _Horizon ("Horizon", Color) = (0.36, 0.66, 0.98, 1)
        _MainTex ("Panorama", 2D) = "black" {}
        _UseTex ("Use Texture", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            fixed4 _Horizon;
            sampler2D _MainTex;
            float _UseTex;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 viewDir : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.viewDir = world - _WorldSpaceCameraPos;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.viewDir);
                float h = saturate(dir.y * 1.05 + 0.02);
                h = pow(h, 0.55);
                fixed4 grad = lerp(_Horizon, _Color, h);
                if (_UseTex < 0.5) return grad;
                float lon = atan2(dir.x, dir.z) / (2.0 * UNITY_PI) + 0.5;
                float lat = asin(clamp(dir.y, -1, 1)) / UNITY_PI + 0.5;
                fixed4 tex = tex2D(_MainTex, float2(lon, lat));
                return lerp(grad, tex, _UseTex);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}

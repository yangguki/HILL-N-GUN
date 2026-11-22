Shader "HillNGun/Proto/Greybox3D/Sky"
{
    Properties
    {
        _Color ("Zenith", Color) = (0.45, 0.72, 0.95, 1)
        _Horizon ("Horizon", Color) = (0.85, 0.78, 0.62, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Front
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _Horizon;

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
                float h = saturate(normalize(i.viewDir).y * 1.35 + 0.12);
                return lerp(_Horizon, _Color, h);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}

Shader "HillNGun/Proto/Greybox3D/Cel"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 n : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float ndl = dot(n, _WorldSpaceLightPos0.xyz) * 0.5 + 0.5;
                ndl = smoothstep(0.25, 0.85, ndl);
                fixed4 c = _Color * (0.55 + 0.45 * ndl);
                return c;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
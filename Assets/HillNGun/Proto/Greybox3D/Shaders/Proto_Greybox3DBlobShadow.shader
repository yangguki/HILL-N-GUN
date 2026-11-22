Shader "HillNGun/Proto/Greybox3D/BlobShadow"
{
    Properties
    {
        _Color ("Color", Color) = (0.12, 0.07, 0.04, 0.45)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = i.uv * 2 - 1;
                float r = length(d);
                float a = saturate(1.0 - smoothstep(0.35, 1.0, r));
                fixed4 c = _Color;
                c.a *= a;
                return c;
            }
            ENDCG
        }
    }
}

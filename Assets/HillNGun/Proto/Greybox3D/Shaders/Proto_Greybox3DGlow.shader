Shader "HillNGun/Proto/Greybox3D/Glow"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.4, 0.1, 1)
        _Intensity ("Intensity", Range(0.5, 8)) = 2.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Intensity;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = _Color;
                c.rgb *= _Intensity;
                c.a = saturate(_Color.a);
                return c;
            }
            ENDCG
        }
    }
}

Shader "HillNGun/Proto/Greybox3D/Vignette"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Vignette ("Vignette", Range(0, 1)) = 0.10
        _Warmth ("Warmth", Range(0, 0.3)) = 0.14
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Vignette;
            float _Warmth;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float2 d = i.uv * 2.0 - 1.0;
                float r = saturate(dot(d, d));
                float vig = 1.0 - _Vignette * r * r;
                c.rgb *= vig;
                c.rgb = lerp(c.rgb, c.rgb * float3(1.06, 1.0, 0.92), _Warmth);
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}

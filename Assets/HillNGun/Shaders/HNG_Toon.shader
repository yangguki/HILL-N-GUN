// HILL N GUN stylized toon shader for the code-built 3D actors (built-in pipeline).
// Vertex colours carry the palette; a global light direction drives a 2-band ramp + soft rim;
// a second inverted-hull pass draws the ink outline using smoothed normals stored in TANGENT.
Shader "HillNGun/Toon"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Flash ("Hit Flash", Range(0,1)) = 0
        _Fade ("Fade", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "TOON"
            Tags { "LightMode"="Always" }
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float4 _HNG_LightDir;      // xyz = direction towards the light (world)
            float4 _HNG_ToonParams;    // x = shadow brightness, y = rim strength, z = band softness, w = highlight
            float4 _HNG_ShadowColor;   // tint multiplied into the shadow band
            fixed4 _Tint;
            float _Flash;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float3 n : TEXCOORD0; float3 v : TEXCOORD1; };

            v2f vert (appdata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                o.pos = UnityObjectToClipPos(i.vertex);
                o.color = i.color;
                o.n = UnityObjectToWorldNormal(i.normal);
                float3 wp = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.v = UNITY_MATRIX_P[3][3] > 0.5 ? -UNITY_MATRIX_V[2].xyz : normalize(_WorldSpaceCameraPos - wp);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 l = normalize(_HNG_LightDir.xyz);
                float ndl = dot(n, l);
                float soft = max(0.001, _HNG_ToonParams.z);
                float lit = smoothstep(-soft, soft, ndl);
                float hi = smoothstep(0.55 - soft, 0.55 + soft, ndl) * _HNG_ToonParams.w;
                float3 shade = lerp(_HNG_ShadowColor.rgb * _HNG_ToonParams.x, float3(1,1,1), lit);
                float3 col = i.color.rgb * shade * (1 + hi);
                float rim = pow(1 - saturate(dot(n, normalize(i.v))), 3) * _HNG_ToonParams.y;
                col += rim * (0.35 + 0.65 * lit);
                col *= _Tint.rgb;
                col = lerp(col, float3(1,1,1), _Flash);
                return fixed4(col, 1);
            }
            ENDCG
        }

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="Always" }
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float4 _HNG_OutlineColor;
            float _HNG_OutlineWidth;   // world units
            float _Flash;

            struct appdata { float4 vertex : POSITION; float4 tangent : TANGENT; fixed4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; };

            v2f vert (appdata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                float3 wp = mul(unity_ObjectToWorld, i.vertex).xyz;
                float3 wn = normalize(mul((float3x3)unity_ObjectToWorld, i.tangent.xyz));
                // i.tangent.w scales the outline per part (0 = no outline for tiny details)
                wp += wn * _HNG_OutlineWidth * i.tangent.w;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1));
                o.color = i.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 c = lerp(_HNG_OutlineColor.rgb, _HNG_OutlineColor.rgb * i.color.rgb * 1.6, _HNG_OutlineColor.a);
                c = lerp(c, float3(1,1,1), _Flash * 0.6);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}

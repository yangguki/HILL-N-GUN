Shader "HillNGun/Proto/Greybox3D/Cel"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Saturation ("Saturation", Range(0.5, 2.5)) = 1.35
        _OutlineColor ("Outline", Color) = (0.12, 0.07, 0.05, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.08)) = 0.018
        _RimColor ("Rim", Color) = (1.0, 0.95, 0.85, 1)
        _RimPower ("Rim Power", Range(1, 8)) = 3.2
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.22
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "UnityCG.cginc"

            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                // World-space outline so FBX meshes with scale 100 don't get a huge hull.
                float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                float3 worldN = UnityObjectToWorldNormal(v.normal);
                worldPos += normalize(worldN) * _OutlineWidth;
                o.pos = UnityWorldToClipPos(worldPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }

        Pass
        {
            Name "CEL"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Saturation;
            fixed4 _RimColor;
            float _RimPower;
            float _RimStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 n : TEXCOORD1;
                float3 wpos : TEXCOORD2;
                fixed4 vcol : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vcol = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 albedo = tex * _Color * i.vcol;
                float grey = dot(albedo.rgb, float3(0.299, 0.587, 0.114));
                albedo.rgb = lerp(grey.xxx, albedo.rgb, _Saturation);

                float3 n = normalize(i.n);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float ndl = dot(n, l) * 0.5 + 0.5;
                float ramp = smoothstep(0.32, 0.72, ndl);
                float shade = 0.52 + 0.48 * ramp;
                fixed3 lit = albedo.rgb * _LightColor0.rgb * shade + albedo.rgb * 0.20;
                float3 view = normalize(_WorldSpaceCameraPos.xyz - i.wpos);
                float rim = pow(1.0 - saturate(dot(n, view)), _RimPower) * _RimStrength;
                lit += rim * _RimColor.rgb;
                return fixed4(lit, 1);
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}

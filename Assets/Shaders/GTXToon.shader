Shader "GTX/ToonCel"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.1,0.1,0.12,1)
        _HighlightColor ("Graphic Highlight", Color) = (1,1,1,1)
        _Steps ("Light Steps", Range(1, 5)) = 3
        _RimThreshold ("Ink Rim Threshold", Range(0, 1)) = 0.82
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _ShadowColor;
            fixed4 _HighlightColor;
            float _Steps;
            float _RimThreshold;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float lightAmount = saturate(dot(normal, lightDirection) * 0.5 + 0.5);
                float steps = max(1.0, _Steps);
                float cel = floor(lightAmount * steps) / steps;
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float rim = step(_RimThreshold, 1.0 - saturate(dot(normal, viewDirection)));
                fixed3 lit = _Color.rgb * _LightColor0.rgb;
                fixed3 color = lerp(_ShadowColor.rgb, lit, cel);
                color = lerp(color, _HighlightColor.rgb, rim * 0.18);
                return fixed4(color, _Color.a);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}

Shader "GTX/ToonCel"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.1,0.1,0.12,1)
        _Steps ("Light Steps", Range(1, 5)) = 3
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
            };

            fixed4 _Color;
            fixed4 _ShadowColor;
            float _Steps;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float lightAmount = saturate(dot(normal, lightDirection) * 0.5 + 0.5);
                float steps = max(1.0, _Steps);
                float cel = floor(lightAmount * steps) / steps;
                fixed3 color = lerp(_ShadowColor.rgb, _Color.rgb * _LightColor0.rgb, cel);
                return fixed4(color, _Color.a);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}

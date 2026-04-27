Shader "GTX/InvertedHullOutline"
{
    Properties
    {
        _Color ("Ink Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" }
        LOD 80
        Cull Front
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}

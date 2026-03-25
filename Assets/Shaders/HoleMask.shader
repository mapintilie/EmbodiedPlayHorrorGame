Shader "UI/HoleMask"
{
    Properties
    {
        _OverlayColor ("Overlay Color", Color) = (0,0,0,0.8)
        _HoleCenter ("Hole Center", Vector) = (0.5,0.5,0,0)
        _HoleRadius ("Hole Radius", Range(0,1)) = 0.2
        _Feather ("Feather", Range(0,0.2)) = 0.02
        _Aspect ("Aspect (w/h)", Float) = 1.77778
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanvasRenderer"="true" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            fixed4 _OverlayColor;
            float4 _HoleCenter;
            float _HoleRadius;
            float _Feather;
            float _Aspect;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 center = _HoleCenter.xy;

                // X mit Aspect skalieren, damit Kreis auf Bildschirm rund erscheint
                float2 diff = float2((uv.x - center.x) * _Aspect, uv.y - center.y);
                float dist = length(diff);

                float t = smoothstep(_HoleRadius - _Feather, _HoleRadius + _Feather, dist);
                fixed4 col = _OverlayColor;
                col.a *= t;

                if (col.a <= 0.001) discard;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}
// Effet d'écran cathodique « bombé léger » (ADR-034), appliqué à l'image finale du jeu (RenderTexture de l'écran),
// affichée par une RawImage plein écran. Même géométrie que Scripts/FX/CrtMath.cs (testée).
// Économe : 4 lectures de texture par pixel (2 pour l'adoucissement, 2 pour la lueur), calculs en half, pas de mip.
Shader "Kenta/CrtDisplay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Image du jeu", 2D) = "black" {}
        _Fish ("Fisheye", Range(0, 0.3)) = 0.06
        _Curve ("Bombé", Range(0, 0.3)) = 0.12
        _Corner ("Coins arrondis", Range(0.001, 0.2)) = 0.05
        _Lines ("Lignes", Range(0, 0.6)) = 0.15
        _LinePeriod ("Période des lignes (px)", Float) = 3
        _Soft ("Adoucissement", Range(0, 0.5)) = 0.15
        _Glow ("Lueur", Range(0, 0.6)) = 0.12
        _Vignette ("Vignette", Range(0, 0.6)) = 0.12
        _Gain ("Gain", Range(0.5, 1.5)) = 1.05
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Opaque" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            half _Fish, _Curve, _Corner, _Lines, _LinePeriod, _Soft, _Glow, _Vignette, _Gain;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // géométrie : fisheye (centre grossi, coins fixes) puis bombé (bords repoussés)
                float2 c = i.uv * 2 - 1;
                c *= 1 - _Fish + _Fish * dot(c, c) * 0.5;
                c *= 1 + (c.yx * _Curve) * (c.yx * _Curve);
                float2 s = c * 0.5 + 0.5;

                // coins arrondis (et noir hors de l'image)
                half2 a = abs(c);
                half rx = _Corner * _MainTex_TexelSize.z * _MainTex_TexelSize.y;   // rayon en x = corner / aspect
                half2 d = max(a - (1 - half2(rx, _Corner)), 0) / half2(rx, _Corner);
                half mask = saturate((1 - length(d)) * 25) * (a.x <= 1 && a.y <= 1);

                // adoucissement horizontal : 2 lectures bilinéaires à ±2·_Soft px, leur moyenne pèse
                // (1 − 2·_Soft) le pixel et _Soft chaque voisin : 3 prises pour le prix de 2
                float2 t = _MainTex_TexelSize.xy;
                float2 o = float2(2 * _Soft * t.x, 0);
                half3 col = (tex2D(_MainTex, s - o).rgb + tex2D(_MainTex, s + o).rgb) * 0.5;
                // lignes de balayage, sur la coordonnée de l'image (elles suivent la courbure)
                col *= 1 - _Lines * (0.5 - 0.5 * cos(6.2831853 * s.y * _MainTex_TexelSize.w / _LinePeriod));
                // lueur : 2 lectures écartées en diagonale (image floutée, bilinéaire)
                half3 glow = (tex2D(_MainTex, s + t * float2(6, 6)).rgb + tex2D(_MainTex, s + t * float2(-6, -6)).rgb) * 0.5;
                col += _Glow * glow;
                // vignette
                half v = sqrt(sqrt(saturate(16 * s.x * s.y * (1 - s.x) * (1 - s.y))));
                col *= (1 - _Vignette + _Vignette * v) * mask * _Gain;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}

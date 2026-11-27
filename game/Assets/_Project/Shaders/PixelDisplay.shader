// Affiche une RenderTexture de calque pixel (couleurs déjà prémultipliées par les shaders Sprite/UI) sans la
// re-prémultiplier : les éléments semi-transparents (voile, flash, Sourdine) gardent leur opacité réelle.
Shader "Kenta/PixelDisplay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Invert ("Inversion (niveau 9 : le monde passe en négatif)", Range(0, 1)) = 0
        _Glitchable ("Reçoit la corruption et le noir et blanc (calque du monde seulement)", Range(0, 1)) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            half _Invert;
            half _Glitchable;
            float4 _ClipRect;
            // Corruption du monde (niveau 9, ADR-042), posée par ScreenGlitch en variables globales (hors Properties) :
            // _KentaCorrupt 0..1 (déjà pulsée sur le temps), _KentaGray 0..1 (noir et blanc),
            // _KentaSeed : x = graine rapide (3 par temps), y = graine lente (un tirage tous les 2 temps : ce qui change
            // de couleur en grand ne bascule jamais plus de 3 fois par seconde), z = temps du morceau (modulo 64),
            // _KentaHero : place du héros dans la
            // texture du calque (texels), autour de laquelle l'image reste propre.
            float _KentaCorrupt;
            float _KentaGray;
            float4 _KentaSeed;
            float4 _KentaHero;

            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            // Lit un texel entier (le pixel art reste net : tout se décale par texels du calque).
            // Le noir et blanc est appliqué à la lecture, avant la corruption : les aplats et barres de couleur pure
            // restent vifs sur un monde déjà décoloré.
            fixed4 Gray(fixed4 c)
            {
                c.rgb = lerp(c.rgb, dot(c.rgb, half3(0.299, 0.587, 0.114)).xxx, _KentaGray * _Glitchable);
                return c;
            }

            fixed4 Texel(float2 px)
            {
                return Gray(tex2Dlod(_MainTex, float4((px + 0.5) * _MainTex_TexelSize.xy, 0, 0)));   // lod 0 : lecture permise dans un if
            }

            // L'image qui se corrompt : bandes décalées (avec leurs couleurs qui se séparent), blocs de sprite déplacés
            // ou réduits à des aplats saturés, pixels morts, barres verticales R/V/B et trame. Tout en texels entiers.
            fixed4 Corrupt(float2 uv, float k)
            {
                float2 size = _MainTex_TexelSize.zw;
                float2 px = floor(uv * size);
                float fast = _KentaSeed.x, slow = _KentaSeed.y;

                // l'image reste propre autour du héros (on ne le perd jamais), la corruption reprend à ~50 texels
                float2 dh = (px - _KentaHero.xy) / float2(44.0, 36.0);
                k *= smoothstep(0.55, 1.25, length(dh));
                // (pas de return anticipé : à k = 0, chaque tirage ci-dessous échoue et l'image reste propre)

                // 1. bandes horizontales de 2 à 9 texels, certaines décalées d'un bloc (la trame « déraille »)
                float bandH = 2.0 + floor(Hash(float2(slow, 3.1)) * 8.0);
                float band = floor(px.y / bandH);
                float hb = Hash(float2(band, fast));
                float2 p = px;
                float split = 0.0;
                if (hb < k * 0.5)
                {
                    p.x += floor((Hash(float2(band, fast + 7.7)) - 0.5) * (6.0 + 60.0 * k));
                    split = 1.0 + floor(Hash(float2(band, fast + 1.3)) * 3.0 * k);
                }
                // une déchirure qui descend l'écran : toute une bande glisse franchement
                float tear = (1.0 - frac(_KentaSeed.z * 0.25)) * size.y;     // un passage tous les 4 temps
                if (k > 0.02 && abs(px.y - tear) < 3.0 + 10.0 * k) { p.x += floor(18.0 * k + 4.0); split = max(split, 2.0); }

                // 2. blocs de sprite corrompus (8 ou 16 texels) : déplacés (rapide) ou aplats saturés (lent)
                float bs = Hash(float2(slow, 9.4)) < 0.5 ? 8.0 : 16.0;
                float2 blk = floor(px / bs);
                float mode = 0.0;
                if (Hash(blk + float2(fast, 2.0)) < k * k * 0.10)
                    p += float2(floor((Hash(blk + fast) - 0.5) * 48.0), floor((Hash(blk + fast + 4.0) - 0.5) * 8.0));
                if (Hash(blk + float2(slow, 5.0)) < k * k * 0.09)
                    mode = 1.0 + floor(Hash(blk + float2(slow, 6.0)) * 2.0);

                // 3. les couleurs se séparent sur les bandes décalées (rouge et bleu de part et d'autre)
                fixed4 c = Texel(p);
                if (split > 0.0)
                {
                    fixed4 cr = Texel(p + float2(split, 0.0));
                    fixed4 cb = Texel(p - float2(split, 0.0));
                    c = fixed4(cr.r, c.g, cb.b, max(c.a, max(cr.a, cb.a)));
                }
                if (mode == 1.0)
                {
                    // aplat saturé : une des 6 couleurs pures (R, V, B, cyan, magenta, jaune), sombre là où l'image l'est
                    float h = floor(Hash(blk + float2(slow, 8.0)) * 6.0);
                    float3 pure = h < 3.0 ? float3(h == 0.0, h == 1.0, h == 2.0) : 1.0 - float3(h == 3.0, h == 4.0, h == 5.0);
                    float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                    c.rgb = pure * c.a * (lum > 0.2 * c.a ? 1.0 : 0.35);
                }
                else if (mode == 2.0) c.rgb = c.gbr;                           // canaux permutés

                // 4. pixels morts : un texel isolé d'une couleur pure
                float hp = Hash(px + fast * 0.13);
                if (hp < k * 0.012)
                {
                    float m = floor(Hash(px + 3.3) * 3.0);
                    c.rgb = float3(m == 0.0, m == 1.0, m == 2.0) * c.a;
                }

                // 5. au plus fort : 3 barres verticales R, V, B (tirées tous les 2 temps) et une trame de lignes
                if (k > 0.55)
                {
                    float a = (k - 0.55) / 0.45 * 0.6;
                    for (float b = 0.0; b < 3.0; b += 1.0)
                    {
                        float x0 = floor(Hash(float2(slow, b + 11.0)) * size.x);
                        float w = 6.0 + floor(Hash(float2(slow, b + 21.0)) * 22.0);
                        if (px.x >= x0 && px.x < x0 + w)
                            c.rgb = lerp(c.rgb, float3(b == 0.0, b == 1.0, b == 2.0) * c.a, a);
                    }
                    if (fmod(px.y, 2.0) < 1.0) c.rgb *= 1.0 - 0.25 * (k - 0.55);
                }
                return c;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float k = _KentaCorrupt * _Glitchable;
                fixed4 c;
                // un vrai if (un ?: de HLSL calcule les deux côtés) : sans corruption, une seule lecture de texture
                UNITY_BRANCH
                if (k > 0.001) c = Corrupt(i.uv, k);
                else c = Gray(tex2Dlod(_MainTex, float4(i.uv, 0, 0)));   // les calques n'ont pas de mipmaps
                c *= i.color;
                c.rgb = lerp(c.rgb, c.a - c.rgb, _Invert);       // négatif d'une couleur prémultipliée
                #ifdef UNITY_UI_CLIP_RECT
                c *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}

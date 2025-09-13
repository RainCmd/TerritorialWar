Shader "Hidden/ShieldImageEffectShader"
{
    Properties
    {
        [PerRendererData]  _MainTex ("Sprite Texture", 2D) = "white" { }
         _Color ("Tint", Color) = (1.000000,1.000000,1.000000,1.000000)
         _StencilComp ("Stencil Comparison", Float) = 8.000000
         _Stencil ("Stencil ID", Float) = 0.000000
         _StencilOp ("Stencil Operation", Float) = 0.000000
         _StencilWriteMask ("Stencil Write Mask", Float) = 255.000000
         _StencilReadMask ("Stencil Read Mask", Float) = 255.000000
         _ColorMask ("Color Mask", Float) = 15.000000
        [Toggle(UNITY_UI_ALPHACLIP)]  _UseUIAlphaClip ("Use Alpha Clip", Float) = 0.000000
    }
    SubShader
    {
        Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" "CanUseSpriteAtlas"="true" "PreviewType"="Plane" }
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                float d = 0.0025;
                fixed4 col = tex2D(_MainTex, i.uv);
                col += tex2D(_MainTex, i.uv + float2(d, d));
                col += tex2D(_MainTex, i.uv + float2(d, -d));
                col += tex2D(_MainTex, i.uv + float2(-d, -d));
                col += tex2D(_MainTex, i.uv + float2(-d, d));
                return col * 0.2;
            }
            ENDCG
        }
    }
}

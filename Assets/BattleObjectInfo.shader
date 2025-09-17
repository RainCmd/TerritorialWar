Shader "Hidden/BattleObjectInfo"
{
    Properties
    {
        _OffsetUV ("UV偏移量", Float) = 0.0005
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;
            float _OffsetUV;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 col_1 = tex2D(_MainTex, i.uv + float2(_OffsetUV, _OffsetUV));
                fixed4 col_2 = tex2D(_MainTex, i.uv + float2(-_OffsetUV, _OffsetUV));
                fixed4 col_3 = tex2D(_MainTex, i.uv + float2(-_OffsetUV, -_OffsetUV));
                fixed4 col_4 = tex2D(_MainTex, i.uv + float2(_OffsetUV, -_OffsetUV));
                fixed2 d = fixed2(col_1.a - col_3.a, col_2.a - col_4.a);
                fixed4 c = dot(d, d);
                col *= i.color;
                return col + c;
            }
            ENDCG
        }
    }
}

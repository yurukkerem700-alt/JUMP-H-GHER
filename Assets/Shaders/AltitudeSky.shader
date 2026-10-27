Shader "KokYankisi/AltitudeSky"
{
    Properties { _MainTex ("Sky", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) _UVRect ("UV Rectangle", Vector) = (0,0,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Color; float4 _UVRect;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                float y=(i.uv.y-_UVRect.y)/_UVRect.w;
                c.a*=smoothstep(0,.13,y)*smoothstep(0,.13,1-y);
                return c;
            }
            ENDCG
        }
    }
}

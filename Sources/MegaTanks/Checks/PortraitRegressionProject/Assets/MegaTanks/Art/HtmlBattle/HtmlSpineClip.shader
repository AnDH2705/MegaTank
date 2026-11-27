Shader "MegaTanks/Spine/World Clip"
{
    Properties { [NoScaleOffset] _MainTex("Atlas",2D)="white"{} _ClipRect("World clip",Vector)=(-100000,-100000,100000,100000) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;fixed4 color:COLOR;};
            sampler2D _MainTex;float4 _ClipRect;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xy;o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target{clip(min(i.world-_ClipRect.xy,_ClipRect.zw-i.world));return tex2D(_MainTex,i.uv)*i.color;}
            ENDCG
        }
    }
}

Shader "MegaTankVFX/GroundFireParticles"
{
 Properties
 {
  _MainTex ("Texture / Flipbook", 2D) = "white" {}
  [HDR] _Tint ("HDR Tint", Color) = (1,1,1,1)
  [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5
  [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 10
 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Cull Off ZWrite Off
  Blend [_SrcBlend] [_DstBlend]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   half4 _Tint;
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Tint; return o; }
   half4 frag(v2f i):SV_Target { return tex2D(_MainTex,i.uv)*i.color; }
   ENDCG
  }
 }
}

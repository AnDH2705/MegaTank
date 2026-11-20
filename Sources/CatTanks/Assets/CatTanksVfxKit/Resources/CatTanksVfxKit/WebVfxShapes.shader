Shader "CatTanks/Web VFX Shapes"
{
    Properties { [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha [_DstBlend], One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            struct Vertex { float4 position:POSITION; float2 uv:TEXCOORD0; float2 shape:TEXCOORD1; fixed4 color:COLOR; };
            struct Pixel { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float2 shape:TEXCOORD1; fixed4 color:COLOR; };

            Pixel Vert(Vertex input)
            {
                Pixel output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.shape = input.shape;
                output.color = input.color;
                return output;
            }

            fixed4 Frag(Pixel input):SV_Target
            {
                float d = length(input.uv);
                float alpha = 1;
                float kind = input.shape.x;
                if (kind > .5 && kind < 1.5) alpha = 1 - smoothstep(1 - fwidth(d), 1, d);
                else if (kind > 1.5 && kind < 2.5)
                    alpha = d < .13 ? lerp(1, .8784, d / .13)
                          : d < .4 ? lerp(.8784, .3333, (d - .13) / .27) : .3333 * saturate((1 - d) / .6);
                else if (kind > 2.5 && kind < 3.5)
                    alpha = d < .45 ? lerp(.7333, .4667, saturate((d - .0469) / .4031))
                          : .4667 * saturate((1 - d) / .55);
                else if (kind > 3.5 && kind < 4.5)
                    alpha = smoothstep(input.shape.y - fwidth(d), input.shape.y, d) * (1 - smoothstep(1 - fwidth(d), 1, d));
                else if (kind > 5.5)
                {
                    const float sector = 6.2831853 / 14;
                    float angle = atan2(input.uv.y, input.uv.x);
                    float edge = d * cos(angle - sector * floor(angle / sector + .5)) / cos(sector * .5);
                    alpha = 1 - smoothstep(1 - fwidth(edge), 1, edge);
                }
                else if (kind > 4.5)
                    alpha = pow(saturate(1 - d), 1.7);
                return fixed4(input.color.rgb, input.color.a * alpha);
            }
            ENDCG
        }
    }
}

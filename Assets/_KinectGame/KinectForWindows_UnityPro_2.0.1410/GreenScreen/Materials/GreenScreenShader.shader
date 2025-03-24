// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
Shader "DX11/GreenScreenShader" {
SubShader {
Pass {

CGPROGRAM
#pragma target 5.0

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

Texture2D _MainTex;
SamplerState sampler_MainTex;  // Unity uses "sampler_" prefix for samplers

StructuredBuffer<float2> depthCoordinates;
StructuredBuffer<float> bodyIndexBuffer;

struct vs_input {
    float4 pos : POSITION;
    float2 tex : TEXCOORD0;
};

struct ps_input {
    float4 pos : SV_POSITION;
    float2 tex : TEXCOORD0;
};

ps_input vert (vs_input v)
{
    ps_input o;
    o.pos = UnityObjectToClipPos(v.pos);
    o.tex = v.tex;
    // Flip x texture coordinate to mimic mirror.
    o.tex.x = 1 - v.tex.x;
    return o;
}

float4 frag (ps_input i, in uint id : SV_InstanceID) : SV_Target
{
    float4 o = float4(0, 1, 0, 1);  // Default to green
    
    int colorWidth = (int)(i.tex.x * 1920.0);
    int colorHeight = (int)(i.tex.y * 1080.0);
    int colorIndex = colorWidth + colorHeight * 1920;

    if ((!isinf(depthCoordinates[colorIndex].x) && !isnan(depthCoordinates[colorIndex].x) && depthCoordinates[colorIndex].x != 0) || 
        (!isinf(depthCoordinates[colorIndex].y) && !isnan(depthCoordinates[colorIndex].y) && depthCoordinates[colorIndex].y != 0))
    {
        // We have valid depth data coordinates. Check the body index buffer.
        float player = bodyIndexBuffer[(int)depthCoordinates[colorIndex].x + (int)(depthCoordinates[colorIndex].y * 512)];
        if (player != 255)
        {
            o = _MainTex.Sample(sampler_MainTex, i.tex);  // Correct sampling
        }
    }

    return o;
}

ENDCG

}
}

Fallback Off
}
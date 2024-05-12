#ifndef POST_PROCESS_BASS_INCLUDED
#define POST_PROCESSING_BASS_INCLUDED

#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Common.hlsl"
#include "NoiseLibrary.hlsl"

TEXTURE2D_X_FLOAT(_CameraDepthTexture);
float4 _BlitTexture_TexelSize;

half4 GetScreenColor(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
}

float GetScreenDepth(float2 uv)
{
    return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_PointClamp, uv);
}

float GetScreenLinear01Depth(float2 uv)
{
    float depth = GetScreenDepth(uv);
    return Linear01Depth(depth, _ZBufferParams);
}

float GetScreenLinearEyeDepth(float2 uv)
{
    float depth = GetScreenDepth(uv);
    return LinearEyeDepth(depth, _ZBufferParams);
}

struct VaryingsRay
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float3 interpolatedRay : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsRay VertRayStereo(Attributes v)
{
    VaryingsRay o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    #if SHADER_API_GLES
        float4 pos = v.positionOS;
        float2 uv = v.uv;
    #else
        float4 pos = GetFullScreenTriangleVertexPosition(v.vertexID);
        float2 uv = GetFullScreenTriangleTexCoord(v.vertexID);
    #endif

    o.positionCS = pos;
    o.texcoord = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
    float3 ray = mul(unity_CameraInvProjection, float4(o.texcoord * 2 - 1, 0, -1)).xyz;
    o.interpolatedRay = mul(unity_CameraToWorld, float4(ray, 0)).xyz;
    return o;
}

float3 GetWorldPositionByRay(float3 interpolatedRay, float linearEyeDepth)
{
    return _WorldSpaceCameraPos.xyz + interpolatedRay * linearEyeDepth;
}

#endif
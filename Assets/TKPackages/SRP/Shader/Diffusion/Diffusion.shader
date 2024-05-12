Shader "Hidden/TK/PostFX/Diffusion"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    TEXTURE2D_X_FLOAT(_CameraDepthTexture);
    
    TEXTURE2D_X(_BlurTex);
    SAMPLER(sampler_BlurTex);

    float _Contrast;
    float _Intensity;

    static const float Weights[9] = {0.5352615, 0.7035879, 0.8553453, 0.9616906, 1, 0.9616906, 0.8553453, 0.7035879, 0.5352615};

    float3 Contrast(float3 In, float Contrast)
    {
        const float midpoint = pow(0.5, 2.2);
        return (In - midpoint) * Contrast + midpoint;
    }

    half4 Frag_Contrast(Varyings input) : SV_Target
    {
        half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp , input.texcoord);

        color.rgb = Contrast(color.rgb, _Contrast);
        
        return color;
    }

    half4 Frag_Blur1(Varyings input) : SV_Target
    {
        half4 color = 0;

        float totalWeight = 0;
        
        for(int i = -4; i <= 4; i++)
        {
            float weight = Weights[i + 4];
            totalWeight += weight;
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float4(i * input.texcoord.x, 0, 0, 0)) * weight;
        }

        color /= totalWeight;
        
        return color;
    }

    half4 Frag_Blur2(Varyings input) : SV_Target
    {
        half4 color = 0;

        float totalWeight = 0;
        
        for(int i = -4; i <= 4; i++)
        {
            float weight = Weights[i + 4];
            totalWeight += weight;
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float4(0, i * input.texcoord.y, 0, 0)) * weight;
        }

        color /= totalWeight;
        
        return color;
    }

    half4 Frag_Blend(Varyings input) : SV_Target
    {
        half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp , input.texcoord);
        half4 blur = SAMPLE_TEXTURE2D_X(_BlurTex, sampler_BlurTex, input.texcoord);

          color.rgb = 1.0 - (1.0 - color.rgb) * (1.0 - blur.rgb * _Intensity);
        // color.rgb = lerp(color.rgb, blur.rgb, _Intensity);
        
        return color;
    }

    half4 Frag_Test(Varyings input) : SV_Target
    {
        half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp , input.texcoord);

        color = half4(color.rgb * _Intensity, 1.0);
        
        return color;
    }
    
    ENDHLSL
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        ZTest Always ZWrite Off Cull Off

        Pass // 0
        {
            Name "Contrast"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag_Contrast
            ENDHLSL
        }
        
        Pass // 1
        {
            Name "Blur1"
            
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag_Blur1
            ENDHLSL
        }     
           
        Pass // 2
        {
            Name "Blur2"
            
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag_Blur2
            ENDHLSL
        }
        
        Pass // 3
        {
            Name "Blend"
            
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag_Blend
            ENDHLSL
        }
        Pass // 4
        {
            Name "Test"
            
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag_Test
            ENDHLSL
        }

    }
}
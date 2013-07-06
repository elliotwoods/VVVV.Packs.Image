//@author: Elliot Woods
//@help: A set of conversion techniques for RGB, YUV with support for mangled 10bit modes
//@tags: DeckLink
//@credits:

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------

//transforms
float4x4 tWVP: WORLDVIEWPROJECTION;

//texture
texture Tex <string uiname="Texture";>;
sampler Samp = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (Tex);          //apply a texture to the sampler
    MipFilter = NONE;         //sampler states
    MinFilter = POINT;
    MagFilter = POINT;
	
	AddressU = CLAMP;
	AddressV = CLAMP;
	
};

int InputWidth <string uiname="Input Width";> = 1920;
int InputHeight <string uiname="Input Height";> = 1080;

int OutputWidth <string uiname="Output Width";> = 1920;
int OutputHeight <string uiname="Output Height";> = 1080;

float3x3 YUV2RGBTransform;

struct vs2ps
{
    float4 Pos  : POSITION;
    float2 TexCd : TEXCOORD0;
};

// --------------------------------------------------------------------------------------------------
// VERTEXSHADERS
// --------------------------------------------------------------------------------------------------
vs2ps VS(
    float4 PosO  : POSITION,
    float4 TexCd : TEXCOORD0)
{
    //declare output struct
    vs2ps Out;

    //transform position
    Out.Pos = mul(PosO, tWVP);
    
	//pass through texture coords
    Out.TexCd = TexCd;

    return Out;
}

// --------------------------------------------------------------------------------------------------
// PIXELSHADERS:
// --------------------------------------------------------------------------------------------------

float2 toOutputIndex(float2 textureCoord)
{
	float2 R = float2(OutputWidth, OutputHeight);
	float2 pixelCoord = textureCoord * R;
	return pixelCoord;
}

float2 toInputCoord(float2 pixelCoord)
{
	float2 R = float2(InputWidth, InputHeight);
	return pixelCoord / R;
}

float Y(float3 RGB)
{
	float value = 0.299 * RGB.r + 0.587 * RGB.g + 0.114 * RGB.b;
	return saturate(value);
}

float U(float3 RGB)
{
	float value = -0.147 * RGB.r - 0.289 * RGB.g + 0.436 * RGB.b;
	return saturate(value + 0.5f);
}

float V(float3 RGB)
{
	float value = 0.615 * RGB.r - 0.515 * RGB.g - 0.100 * RGB.b;
	return saturate(value + 0.5f);
}

float3 YUV(float3 RGB)
{
	return float3(Y(RGB), U(RGB), V(RGB));
}

float4 PSPassthrough(vs2ps In) : COLOR
{
	return tex2D(Samp, In.TexCd);
}

float4 PSRGB888_to_YUV444_8(vs2ps In): COLOR
{
	float3 rgb = tex2D(Samp, In.TexCd).rgb;
	
	float y = Y(rgb);
	float u = U(rgb);
	float v = V(rgb);
	
	return float4(y, u, v, 1.0f);
}

float4 PSRGB888_to_YUV422_8(vs2ps In): COLOR
{
	float2 outputIndex = toOutputIndex(In.TexCd);

	float2 inputAIndex = outputIndex * float2(2.0f, 1.0f);
	float2 inputBIndex = inputAIndex + float2(1.0f, 0.0f);
	
	float2 inputACoord = toInputCoord(inputAIndex);
	float2 inputBCoord = toInputCoord(inputBIndex);
	
	inputACoord.y += 0.00000000001; //really strange effect?
	//perhaps thinks we're in norm coords or something?
	//this also affects both A and B lookups??
	
	float3 rgbA = tex2D(Samp, inputACoord).rgb;
	float3 rgbB = tex2D(Samp, inputBCoord).rgb;
	
	float3 yuvA = YUV(rgbA);
	float3 yuvB = YUV(tex2D(Samp, inputBCoord).rgb);
	
	float y1 = yuvA.r;
	float y2 = yuvB.r;
	float u = (yuvA.g + yuvB.g) / 2.0f;
	float v = (yuvA.b + yuvB.b) / 2.0f;
	
	return float4(v, y1, u, y2);
}

float4 PSYUV444_to_RGB888_8(vs2ps In) : COLOR
{
	float3 yuv = tex2D(Samp, In.TexCd).rgb;
	yuv[0] -= 16.0f / 255.0f;
	
	float3 rgb = mul(yuv, YUV2RGBTransform);
	
	return float4(rgb.r, rgb.g, rgb.b, 1.0f);
}

float4 PSYUV422_to_RGB888_8(vs2ps In) : COLOR
{
	int2 outputIndex = (int2) (toOutputIndex(In.TexCd) + float2(0,0));
	int2 inputIndex = outputIndex;
	inputIndex.x /= 2;
	float2 inputCoord = toInputCoord((float2)inputIndex);
	
	float4 yuv2 = tex2D(Samp, inputCoord).rgba;
	
	bool rightPixel = outputIndex.x % 2;
	
	float y = rightPixel ? yuv2.a : yuv2.g;
	float u = yuv2.b - 0.5f;
	float v = yuv2.r - 0.5f;
	
	y -= 16.0f / 255.0f;
	
	float3 rgb = saturate(mul(float3(y,u,v), YUV2RGBTransform));
	return float4(rgb.r, rgb.g, rgb.b, 1.0f);
}

float4 PSNotSupported(vs2ps In) : COLOR
{
	float2 outputIndex = toOutputIndex(In.TexCd);
	outputIndex /= 10.0f;
	return sin(outputIndex.x + outputIndex.y);
}

// --------------------------------------------------------------------------------------------------
// TECHNIQUES:
// --------------------------------------------------------------------------------------------------

technique Passthrough
{
    pass P0
    {
        VertexShader = compile vs_1_1 VS();
        PixelShader  = compile ps_2_0 PSPassthrough();
    }
}

technique RGB888_to_YUV444_8
{
    pass P0
    {
        VertexShader = compile vs_1_1 VS();
        PixelShader  = compile ps_2_0 PSRGB888_to_YUV444_8();
    }
}

technique RGB888_to_YUV422_8
{
    pass P0
    {
        VertexShader = compile vs_1_1 VS();
        PixelShader  = compile ps_3_0 PSRGB888_to_YUV422_8();
    }
}

technique YUV444_to_RGB888_8
{
    pass P0
    {
        VertexShader = compile vs_1_1 VS();
        PixelShader  = compile ps_2_0 PSYUV444_to_RGB888_8();
    }
}

technique YUV422_to_RGB888_8
{
    pass P0
    {
        VertexShader = compile vs_1_1 VS();
        PixelShader  = compile ps_3_0 PSYUV422_to_RGB888_8();
    }
}

technique NotSupported
{
    pass P0
    {
        VertexShader = compile vs_1_1 VS();
        PixelShader  = compile ps_2_0 PSNotSupported();
    }
}
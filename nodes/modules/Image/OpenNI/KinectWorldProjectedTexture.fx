//@author: Elliot Woods
//@help: Apply normals and lighting effects to DepthMesh
//@tags:
//@credits

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------
#include <effects\undistort.fxh>

texture TexRGB <string uiname="RGB";>;
sampler SampRGB = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (TexRGB);          //apply a texture to the sampler
    MipFilter = LINEAR;         //sampler states
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

#include "DepthMesh.fxh"

// --------------------------------------------------------------------------------------------------
// PIXELSHADERS:
// --------------------------------------------------------------------------------------------------
float Alpha = 1.0f;

float4 PSPreview(vs2ps In): COLOR
{
    float4 col = tex2D(SampRGB, In.TexCd);
	col.a = Alpha * (In.existence > drop);
    return col;
}

float4 PSXYZ(vs2ps In): COLOR
{
    float4 col = 1;
	col.rgb = In.PosW;
	col.a = Alpha * (In.existence > drop);
    return col;
}

float4x4 tProjector;
float4 PSProjectedTexture(vs2ps In) : COLOR
{
	float4 PosW;
	PosW.xyz = In.PosW;
	PosW.w = 1.0f;
	float4 TexCd = mul(PosW, tProjector);
	TexCd /= TexCd.w;
	
	TexCd.y *= -1.0f;
	TexCd.xy += 1.0f;
	TexCd.xy /= 2.0f;
	
    float4 col = tex2D(SampRGB, TexCd.xy);
	
	
	//col.rg = TexCd.xy;
	//col.b = 0.0f;
	
	col.rgb *= TexCd.x >= 0 && TexCd.x <= 1.0f;
	col.rgb *= TexCd.y >= 0 && TexCd.y <= 1.0f;
	
	col.a = Alpha * (In.existence > drop);
    return col;	
}

float2 FocalLength;
float2 PrincipalPoint;
float4 Distortion;
float2 Resolution;

float4 PSProjectedTextureUndistorted(vs2ps In) : COLOR
{
	float4 PosW;
	PosW.xyz = In.PosW;
	PosW.w = 1.0f;
	float4 TexCd = mul(PosW, tProjector);
	TexCd /= TexCd.w;
	
	TexCd.y *= -1.0f;
	TexCd.xy += 1.0f;
	TexCd.xy /= 2.0f;
	
	TexCd.xy = Undistort(TexCd.xy, FocalLength, PrincipalPoint, Distortion, Resolution);
    float4 col = tex2D(SampRGB, TexCd.xy);
	
	
	//col.rg = TexCd.xy;
	//col.b = 0.0f;
	
	col.rgb *= TexCd.x >= 0 && TexCd.x <= 1.0f;
	col.rgb *= TexCd.y >= 0 && TexCd.y <= 1.0f;
	
	col.a = Alpha * (In.existence > drop);
    return col;	
}



// --------------------------------------------------------------------------------------------------
// TECHNIQUES:
// --------------------------------------------------------------------------------------------------

technique TRGB
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PSPreview();
    }
}

technique TRGBProjected
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PSProjectedTexture();
    }
}

technique TRGBUndistortProjected
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PSProjectedTextureUndistorted();
    }
}


technique TXYZ
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PSXYZ();
    }
}
//@author: Elliot Woods
//@help: Apply normals and lighting effects to DepthMesh
//@tags:
//@credits

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------

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

technique TXYZ
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PSXYZ();
    }
}
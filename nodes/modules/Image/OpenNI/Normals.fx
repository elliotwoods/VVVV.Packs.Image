//@author: Elliot Woods
//@help: Take differential of world to generate normals
//@tags:
//@credits:

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------

//texture
texture Tex <string uiname="World Texture";>;
sampler Samp = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (Tex);          //apply a texture to the sampler
    MipFilter = LINEAR;         //sampler states
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

//the data structure: "vertexshader to pixelshader"
//used as output data with the VS function
//and as input data with the PS function
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
    Out.Pos = PosO;
    
    //transform texturecoordinates
    Out.TexCd = TexCd;

	Out.Pos.xy *= 2;
	
    return Out;
}

// --------------------------------------------------------------------------------------------------
// PIXELSHADERS:
// --------------------------------------------------------------------------------------------------

float3 GetWorld(float2 xy)
{
	return tex2D(Samp, xy);
}

float spread = 5;
float2 CameraResolution = float2(640,480);
float4 PS(vs2ps In): COLOR
{
	float3 v[4];
	
	float2 x = In.TexCd.xy;
	float2 dx = spread * 0.5f / CameraResolution * float2(1.0f, 0);
	float2 dy = spread * 0.5f / CameraResolution  * float2(0.0f, 1.0f);
	v[0] = GetWorld(x-dx-dy);
	v[1] = GetWorld(x+dx-dy);
	v[2] = GetWorld(x-dx+dy);
	v[3] = GetWorld(x+dx+dy);
	
	float3 r[2];
	r[0] =  cross(v[2] - v[0], v[1] - v[0]);
	r[1] =  cross(v[3] - v[2], v[1] - v[3]);
    float4 col = (float4)1;
	col.rgb = normalize(r[0] + r[1]);
    return col;
}

// --------------------------------------------------------------------------------------------------
// TECHNIQUES:
// --------------------------------------------------------------------------------------------------

technique TNormals
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PS();
    }
}
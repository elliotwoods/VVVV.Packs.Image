//@author: Elliot Woods
//@help: Include file for rendering Kinect data as mesh with holes (needs pixel shader)
//@tags:
//@credits: vvvv group

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------

//transforms
float4x4 tW: WORLD;        //the models world matrix
float4x4 tV: VIEW;         //view matrix as set via Renderer (EX9)
float4x4 tP: PROJECTION;
float4x4 tWVP: WORLDVIEWPROJECTION;

//texture
texture Tex <string uiname="World";>;
sampler Samp = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (Tex);          //apply a texture to the sampler
    MipFilter = POINT;         //sampler states
    MinFilter = POINT;
    MagFilter = NONE;
	AddressU = BORDER;
	AddressV = BORDER;
};

const float deadzone = 0.2;
const float maxjump=0.05; // Jump coefficient
const float drop = 0.9;

//the data structure: "vertexshader to pixelshader"
//used as output data with the VS function
//and as input data with the PS function
struct vs2ps
{
    float4 Pos  : POSITION;
    float2 TexCd : TEXCOORD0;
	float existence : TEXCOORD1;
	float3 PosW : TEXCOORD2;
	float4 PosP : TEXCOORD3;
};

// --------------------------------------------------------------------------------------------------
// VERTEXSHADERS
// --------------------------------------------------------------------------------------------------
bool jumps (float2 TexCd)
{
	float o  = false;
	float2 dv = 1.0f / float2(640.0f, 480.0f);
	int steps = 1;
	float r = length(tex2Dlod(Samp, float4(TexCd.x, TexCd.y, 0, 0)).xyz);
	float r2;
	for (float x = -dv.x * steps + TexCd.x; x <= dv.x * steps + TexCd.x; x+= dv.x)
	{
		for (float y = -dv.y * steps + TexCd.y; y <= dv.y * steps + TexCd.y; y+= dv.y)
		{
			r2 = length(tex2Dlod(Samp, float4(x, y, 0, 0)).xyz);
			if (r - r2 > maxjump * r || r2 <= deadzone)
				o = true;
		}
	}
	
	return o;
}
vs2ps VS(
    float4 TexCd : TEXCOORD0)
{
    //declare output struct
    vs2ps Out;

    //transform texturecoordinates
    Out.TexCd = TexCd;
	
	float4 TexCdl = float4(TexCd.x, TexCd.y, 0, 0);
	float4 PosO = tex2Dlod(Samp, TexCdl);
	float4 p = mul(PosO,tWVP);
	bool zero = jumps(TexCd);
	
	p.w *= !zero;
	p.z = zero ? 5 : p.z;
	
	Out.Pos = p;
	Out.PosP = p;

	Out.existence = !zero;
	Out.PosW = mul(PosO,tW).xyz;
	
    return Out;
}
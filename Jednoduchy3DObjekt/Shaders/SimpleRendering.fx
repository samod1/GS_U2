float4x4 WorldViewProj : WorldViewProjection;   //T�to kon�tanta bude reprezentova� transforma�n� maticu pre prepo�et 3D svetov�ch s�radn�c na obrazovkov�

struct VS_IN
{
	float3 pos : POSITION;   //S�radnice x,y,z netransformovan� v WCS
	float4 col : COLOR0;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR0;
};

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(float4(input.pos.xyz, 1.0), WorldViewProj);  //predpis pre v�po�et obrazovkov�ch s�radn�c
	output.col = input.col;

	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	return input.col;
}

technique10 Render
{
	pass P0
	{
		SetGeometryShader(0);
		SetVertexShader(CompileShader(vs_5_0, VS()));
		SetPixelShader(CompileShader(ps_5_0, PS()));
	}
}
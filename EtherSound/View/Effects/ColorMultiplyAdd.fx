sampler2D Input : register(s0);

float4 Factor : register(c0);
float4 Addend : register(c1);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 color = tex2D(Input, uv);
	
	color = color * Factor + color.a * Addend;

	return color; //  float4(color.rgb * color.a, color.a);
}
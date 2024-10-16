#[compute]
#version 460

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(push_constant, std430) uniform Params {
	vec2 effect_size;
} params;

layout(set = 0, binding = 0, rgba16f) uniform restrict readonly image2D in_image;
layout(set = 0, binding = 1, rgba16f) uniform restrict writeonly image2D out_image;

void main()
{
	if ( any( greaterThanEqual( gl_GlobalInvocationID.xy, params.effect_size ) ) ) return;

	imageStore( out_image, ivec2( gl_GlobalInvocationID.xy ), imageLoad( in_image, ivec2( gl_GlobalInvocationID.xy ) ) );
}
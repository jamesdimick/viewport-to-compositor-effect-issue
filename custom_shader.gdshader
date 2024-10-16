shader_type spatial;
render_mode unshaded, depth_draw_always;

uniform float radius : hint_range( 0.0, 100.0 ) = 1.0;

varying vec3 world_position;

float sphi( vec3 ro, vec3 rd, vec4 sph )
{
	vec3 oc = ro - sph.xyz;
	float b = dot( oc, rd );
	float c = dot( oc, oc ) - sph.w * sph.w;
	float h = b * b - c;
	if ( h < 0.0 ) return -1.0;
	h = sqrt( h );
	return -b - h;
}

void vertex()
{
	world_position = ( MODEL_MATRIX * vec4( VERTEX, 1.0 ) ).xyz;

	vec4 vp = MODELVIEW_MATRIX * vec4( VERTEX, 1.0 );
	float d = -vp.z;
	POINT_SIZE = ( ( radius * ( VIEWPORT_SIZE.y * 2.0 ) ) * ( 1.0 / cos( atan( length( vp.xy ) / d ) ) ) / d );
}

void fragment()
{
	vec3 ro = INV_VIEW_MATRIX[3].xyz;
	vec3 rd = normalize( ( INV_VIEW_MATRIX * vec4( ( INV_PROJECTION_MATRIX * vec4( ( FRAGCOORD.xy / VIEWPORT_SIZE ) * 2.0 - 1.0, -1.0, 1.0 ) ).xy, -1.0, 0.0 ) ).xyz );
	float dist = sphi( ro, rd, vec4( world_position, radius ) );

	ALBEDO = dist < 0.0 ? vec3( 0.0 ) : vec3( 1.0 );
}
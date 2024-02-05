shader_type canvas_item;

uniform vec4 u_colorWhite : source_color;
uniform vec4 u_colorBlack : source_color;

void fragment() {
	// get the UV in the 8x8 grid
	vec2 gridUV = UV * 8.0;
	
	// get the square index
	ivec2 square = ivec2(floor(gridUV));
	
	// white -> (i + j) % 2 == 0, black -> (1 + j) % 2 == 1
	COLOR = mix(u_colorWhite, u_colorBlack, mod(float(square.x + square.y), 2.0));
}

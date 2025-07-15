shader_type canvas_item;
render_mode blend_mix;

uniform vec2 center = vec2(0.5, 0.5); // Center in UV coordinates
uniform float radius = 0.0; // Current radius in pixels
uniform float max_radius = 480.0; // Updated to match larger effect size
uniform float intensity = 0.0;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;

void fragment() {
	// Work in UV space (0-1)
	vec2 uv = UV;
	vec2 resolution = vec2(3840.0, 2160.0);
	
	// Convert UV to pixel coordinates for distance calculation
	vec2 pixel_pos = uv * resolution;
	vec2 center_pixel = center * resolution;
	
	// Calculate distance from center in pixels
	float dist = distance(pixel_pos, center_pixel);
	
	// Calculate displacement
	vec2 displacement = vec2(0.0);
	
	if (radius > 0.0 && intensity > 0.0) {
		// Create expanding ring
		float ring_width = 100.0; // Width of the distortion ring in pixels (increased from 60)
		float ring_dist = abs(dist - radius);
		
		// Smooth falloff for the ring
		float ring_falloff = 1.0 - smoothstep(0.0, ring_width, ring_dist);
		
		// Only apply displacement if we're near the ring
		if (ring_falloff > 0.0) {
			// Calculate direction from center
			vec2 direction = normalize(pixel_pos - center_pixel);
			
			// Create displacement that pushes outward from center
			float displacement_strength = ring_falloff * intensity * 0.03; // Increased from 0.02 for more pronounced effect
			
			// Add some wave-like distortion
			float wave = sin(dist * 0.1) * 0.5 + 0.5;
			displacement_strength *= (0.7 + wave * 0.3);
			
			// Fade out as radius increases
			float fade_out = 1.0 - smoothstep(0.0, max_radius, radius);
			displacement_strength *= fade_out;
			
			// Apply displacement in UV space
			displacement = direction * displacement_strength;
		}
	}
	
	// Sample the screen texture with displaced UV
	vec2 displaced_uv = SCREEN_UV + displacement;
	vec4 screen_color = texture(screen_texture, displaced_uv);
	
	// Output the displaced pixels
	COLOR = screen_color;
} 
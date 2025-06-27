shader_type canvas_item;

uniform float fade_size : hint_range(0.0, 0.5) = 0.05;
uniform float fade_power : hint_range(0.5, 4.0) = 2.5;
uniform vec4 background_color : source_color = vec4(0.0, 0.0, 0.0, 0.85);

void fragment() {
    // Calculate distance from edges
    float dist_from_left = UV.x;
    float dist_from_right = 1.0 - UV.x;
    float dist_from_top = UV.y;
    float dist_from_bottom = 1.0 - UV.y;
    
    // Find minimum distance to any edge
    float min_dist = min(min(dist_from_left, dist_from_right), min(dist_from_top, dist_from_bottom));
    
    // Create fade factor that ensures complete fade at edges
    float fade_factor = clamp(min_dist / fade_size, 0.0, 1.0);
    
    // Apply power curve for smoother fade
    fade_factor = pow(fade_factor, fade_power);
    
    // Apply background color with fade
    COLOR = background_color;
    COLOR.a *= fade_factor;
} 
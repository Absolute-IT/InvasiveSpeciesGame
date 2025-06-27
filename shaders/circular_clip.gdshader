shader_type canvas_item;

void fragment() {
    vec2 center = vec2(0.5, 0.5);
    float dist = distance(UV, center);
    
    // Get the texture color
    vec4 tex_color = texture(TEXTURE, UV);
    
    // Only show pixels within the circle (radius 0.5)
    if (dist <= 0.5) {
        COLOR = tex_color;
        // Smooth edge for anti-aliasing
        float edge = 0.49;
        float smoothing = 0.01;
        COLOR.a *= smoothstep(edge + smoothing, edge - smoothing, dist);
    } else {
        COLOR.a = 0.0;
    }
}
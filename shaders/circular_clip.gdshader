shader_type canvas_item;

void fragment() {
    vec2 center = vec2(0.5, 0.5);
    float dist = distance(UV, center);

    // Sample texture and apply the node's modulate (COLOR is the per-CanvasItem modulate)
    vec4 tex_color = texture(TEXTURE, UV);
    vec4 final_color = tex_color * COLOR;

    // Circular mask with smooth edge for anti-aliasing
    float edge = 0.49;
    float smoothing = 0.01;
    float inside = step(dist, 0.5);
    float smooth_alpha = mix(0.0, smoothstep(edge + smoothing, edge - smoothing, dist), inside);
    final_color.a *= smooth_alpha;

    // Single assignment at end per shader rules
    COLOR = final_color;
}
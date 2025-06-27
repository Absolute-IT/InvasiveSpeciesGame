shader_type canvas_item;

uniform float inner_radius : hint_range(0.0, 1.0) = 0.8;
uniform float outer_radius : hint_range(0.0, 1.0) = 1.0;
uniform vec4 ring_color : source_color = vec4(1.0, 0.0, 0.0, 1.0);

void fragment() {
    vec2 center = vec2(0.5, 0.5);
    float dist = distance(UV, center);
    
    if (dist >= inner_radius && dist <= outer_radius) {
        COLOR = ring_color;
    } else {
        COLOR.a = 0.0;
    }
} 
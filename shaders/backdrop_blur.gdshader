shader_type canvas_item;

uniform float blur_amount : hint_range(0.0, 10.0) = 3.0;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;

void fragment() {
    vec2 screen_uv = SCREEN_UV;
    vec2 screen_pixel_size = SCREEN_PIXEL_SIZE;
    
    // Box blur implementation
    vec4 blurred_color = vec4(0.0);
    float total_weight = 0.0;
    
    // Sample in a grid pattern for blur
    int blur_samples = int(blur_amount);
    for (int x = -blur_samples; x <= blur_samples; x++) {
        for (int y = -blur_samples; y <= blur_samples; y++) {
            vec2 offset = vec2(float(x), float(y)) * screen_pixel_size * 2.0;
            vec4 sample_color = texture(screen_texture, screen_uv + offset);
            
            // Use gaussian-like weight based on distance
            float weight = 1.0 / (1.0 + length(vec2(float(x), float(y))) * 0.5);
            blurred_color += sample_color * weight;
            total_weight += weight;
        }
    }
    
    blurred_color /= total_weight;
    
    // Mix the blurred background with the original color
    COLOR = mix(blurred_color, COLOR, COLOR.a);
} 
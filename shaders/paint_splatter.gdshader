shader_type canvas_item;

uniform sampler2D entity_texture;
uniform vec4 ring_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float splatter_scale : hint_range(1.0, 5.0) = 2.5;
uniform vec2 entity_center = vec2(0.5, 0.5);
uniform float entity_scale = 0.15;

// Generate pseudo-random value
float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

// Generate noise for splatter pattern
float noise(vec2 st) {
    vec2 i = floor(st);
    vec2 f = fract(st);
    
    float a = random(i);
    float b = random(i + vec2(1.0, 0.0));
    float c = random(i + vec2(0.0, 1.0));
    float d = random(i + vec2(1.0, 1.0));
    
    vec2 u = f * f * (3.0 - 2.0 * f);
    
    return mix(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

// Create splatter pattern
float splatter_pattern(vec2 uv, vec2 center, float time) {
    vec2 dir = uv - center;
    float dist = length(dir);
    float angle = atan(dir.y, dir.x);
    
    // Create base splatter burst
    float burst_radius = time * 0.2; // Smaller burst
    float burst = smoothstep(burst_radius + 0.05, burst_radius - 0.05, dist);
    burst *= smoothstep(0.0, 0.1, time); // Fade in
    
    // Create multiple splatter tendrils with varying lengths
    float splatter = 0.0;
    for (int i = 0; i < 12; i++) {
        float tendril_angle = float(i) * 0.523 + noise(vec2(float(i))) * 0.3; // ~30 degrees + random
        vec2 tendril_dir = vec2(cos(tendril_angle), sin(tendril_angle));
        
        // Calculate how aligned this pixel is with the tendril direction
        float alignment = dot(normalize(dir), tendril_dir);
        
        // Add noise for irregular shape
        float noise_val = noise(uv * 8.0 + vec2(float(i) * 3.14));
        float tendril_length = 0.2 + noise_val * 0.2 + random(vec2(float(i))) * 0.2; // Shorter tendrils
        
        // Create tendril with varying width
        float tendril_width = 0.15 - dist * 0.1;
        float tendril_strength = smoothstep(tendril_width, 0.0, 1.0 - alignment);
        
        // Apply distance falloff based on tendril length
        float max_dist = tendril_length * time * splatter_scale;
        float distance_falloff = smoothstep(max_dist, max_dist * 0.3, dist);
        
        splatter = max(splatter, tendril_strength * distance_falloff);
    }
    
    // Add scattered droplets
    float droplets = 0.0;
    vec2 droplet_uv = uv * 20.0;
    float droplet_noise = noise(droplet_uv * 0.5);
    
    // Create droplets that appear over time
    if (droplet_noise > 0.7 && time > 0.2) {
        vec2 droplet_cell = floor(droplet_uv);
        vec2 droplet_pos = fract(droplet_uv);
        
        // Random offset for each droplet
        vec2 offset = vec2(random(droplet_cell), random(droplet_cell + vec2(1.0, 0.0))) - 0.5;
        droplet_pos += offset * 0.3;
        
        float droplet_dist = length(droplet_pos - 0.5);
        float droplet_size = 0.15 + random(droplet_cell + vec2(0.0, 1.0)) * 0.1;
        
        // Only show droplets within a certain radius of center
        float center_dist = length((droplet_cell + droplet_pos) / 20.0 - center);
        if (center_dist < time * splatter_scale * 0.5) { // Smaller droplet radius
            droplets = smoothstep(droplet_size, 0.0, droplet_dist);
        }
    }
    
    return max(burst, max(splatter, droplets));
}

void fragment() {
    vec2 uv = UV;
    
    // Initialize output color as transparent
    vec4 final_color = vec4(0.0, 0.0, 0.0, 0.0);
    
    // Check if we have valid data - both entity texture and ring color
    vec4 entity_test = texture(entity_texture, vec2(0.5, 0.5));
    bool has_valid_data = entity_test.a > 0.01 && (length(ring_color.rgb) > 0.01 || ring_color.a > 0.01);
    
    // Only process if we have valid data
    if (has_valid_data) {
        // Calculate edge fade to prevent hard cutoff
        float edge_fade = 1.0;
        float edge_distance = 0.15; // Fade starts at 15% from edge
        edge_fade *= smoothstep(0.0, edge_distance, uv.x);
        edge_fade *= smoothstep(0.0, edge_distance, uv.y);
        edge_fade *= smoothstep(0.0, edge_distance, 1.0 - uv.x);
        edge_fade *= smoothstep(0.0, edge_distance, 1.0 - uv.y);
        
        // Calculate splatter pattern
        float splatter = splatter_pattern(uv, entity_center, progress);
        
        // For the initial frames, show the entity being squashed/splattered
        if (progress < 0.1) {
            // Calculate distance from center
            vec2 offset = (uv - entity_center) / entity_scale;
            
            // Apply a squash effect
            float squash = 1.0 + progress * 5.0;
            offset.x *= squash;
            offset.y /= squash;
            
            vec2 entity_uv = entity_center + offset * entity_scale;
            
            // Sample entity if within bounds
            if (entity_uv.x >= 0.0 && entity_uv.x <= 1.0 && entity_uv.y >= 0.0 && entity_uv.y <= 1.0) {
                vec4 entity_sample = texture(entity_texture, entity_uv);
                if (entity_sample.a > 0.1) {
                    final_color = entity_sample;
                }
            }
        }
        
        // Only apply splatter if we haven't drawn the entity
        if (final_color.a < 0.1) {
            // Sample entity texture for color information
            vec4 entity_color = texture(entity_texture, vec2(0.5, 0.5));
            
            // Create varied splatter colors
            float color_variation = noise(uv * 20.0);
            vec3 splatter_color = mix(entity_color.rgb, ring_color.rgb, 0.3 + color_variation * 0.4);
            
            // Darken the color slightly for paint effect
            splatter_color *= 0.85;
            
            // Apply fade out over time
            float fade = 1.0 - smoothstep(0.5, 1.0, progress);
            
            // Set splatter color with transparency
            final_color = vec4(splatter_color, splatter * fade);
        }
        
        // Apply edge fade to prevent hard cutoff
        final_color.a *= edge_fade;
    }
    
    COLOR = final_color;
} 
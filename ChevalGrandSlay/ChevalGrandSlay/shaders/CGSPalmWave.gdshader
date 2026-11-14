shader_type canvas_item;

/** 手掌附近蓝带的最大位移，单位为底图像素。 */
uniform float wave_amplitude : hint_range(0.0, 40.0, 0.5) = 26.0;
/** 主波的长度，单位为底图像素。 */
uniform float wavelength : hint_range(200.0, 1000.0, 1.0) = 570.0;
/** 其余蓝带相对手掌蓝带的波动强度。 */
uniform float background_wave_strength : hint_range(0.0, 1.5, 0.05) = 0.85;
uniform float wave_phase : hint_range(0.0, 1.0) = 0.0;

varying vec4 vertex_tint;

void vertex() {
    vertex_tint = COLOR;
}

float cyan_amount(vec3 color) {
    return min(color.g, color.b) - color.r;
}

vec2 ribbon_offset(vec2 pixel, vec2 tangent, float phase) {
    vec2 normal = vec2(-tangent.y, tangent.x);
    float along = dot(pixel, tangent);
    float across = dot(pixel, normal);
    float primary_wave = sin(TAU * along / wavelength - phase);
    float secondary_wave = sin(TAU * along / (wavelength * 0.55) - 2.0 * phase + across * 0.014);
    return normal * (0.74 * primary_wave + 0.26 * secondary_wave);
}

void fragment() {
    vec4 original = COLOR;
    // Protect the eye and the entire thin streak up to its tip, with a soft margin.
    float wave_area = max(smoothstep(0.435, 0.49, UV.x), smoothstep(0.435, 0.49, UV.y));

    if (wave_area > 0.0) {
        float distance_from_band = UV.y - (1.58 - 1.31 * UV.x);
        float foreground = smoothstep(0.39, 0.49, UV.x)
            * smoothstep(-0.135, -0.055, distance_from_band)
            * (1.0 - smoothstep(0.17, 0.30, distance_from_band));
        vec2 pixel = UV / TEXTURE_PIXEL_SIZE;
        float phase = TAU * wave_phase;
        // The bands behind the character rise more steeply on the left.
        vec2 background_tangent = normalize(mix(vec2(0.60, -0.80), vec2(0.77, -0.64), UV.x));
        vec2 background_offset = ribbon_offset(pixel, background_tangent, phase + 1.4)
            * background_wave_strength;
        vec2 foreground_offset = ribbon_offset(pixel, vec2(0.85, -0.527), phase);
        vec2 displacement = mix(background_offset, foreground_offset, foreground)
            * (wave_amplitude * wave_area);
        vec2 warped_uv = clamp(UV + displacement * TEXTURE_PIXEL_SIZE, vec2(0.0), vec2(1.0));

        vec4 source = texture(TEXTURE, UV);
        vec4 warped = texture(TEXTURE, warped_uv);
        // Include cyan at either sample so the colored edge can move in both directions.
        float cyan_mask = smoothstep(0.015, 0.105, max(cyan_amount(source.rgb), cyan_amount(warped.rgb)));
        // Keep dark ink contours from bending with the light.
        float contour_mask = smoothstep(0.09, 0.26, min(source.g, warped.g));
        COLOR = mix(original, warped * vertex_tint, wave_area * cyan_mask * contour_mask);
    }
}

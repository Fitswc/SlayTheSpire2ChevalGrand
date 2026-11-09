shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform vec4 aura_color : source_color = vec4(0.12, 0.55, 1.0, 1.0);
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform float outline_width : hint_range(1.0, 8.0) = 2.0;
uniform float trail_length : hint_range(0.0, 40.0) = 22.0;

void fragment() {
    // CanvasGroup provides the complete animated silhouette on a transparent buffer.
    vec4 body = textureLod(screen_texture, SCREEN_UV, 0.0);
    if (body.a > 0.0001) {
        body.rgb /= body.a;
    }

    float outline = 0.0;
    for (int i = 0; i < 12; i++) {
        float angle = float(i) * 6.2831853 / 12.0;
        vec2 offset = vec2(cos(angle), sin(angle)) * outline_width * SCREEN_PIXEL_SIZE;
        outline = max(outline, textureLod(screen_texture, SCREEN_UV + offset, 0.0).a);
    }

    float trail = 0.0;
    for (int i = 1; i <= 12; i++) {
        float distance_ratio = float(i) / 12.0;
        // Sampling to the right extends the silhouette toward screen-left.
        vec2 offset = vec2(trail_length * distance_ratio, 0.0) * SCREEN_PIXEL_SIZE;
        float sample_alpha = textureLod(screen_texture, SCREEN_UV + offset, 0.0).a;
        trail = max(trail, sample_alpha * pow(1.0 - distance_ratio, 1.5) * 0.65);
    }

    float aura_alpha = max(outline * 0.9, trail) * aura_color.a * intensity;
    // Composite the aura behind the body, preserving the character's original colors.
    float alpha = body.a + aura_alpha * (1.0 - body.a);
    vec3 rgb = (body.rgb * body.a + aura_color.rgb * aura_alpha * (1.0 - body.a))
        / max(alpha, 0.0001);
    COLOR *= vec4(rgb, alpha);
}

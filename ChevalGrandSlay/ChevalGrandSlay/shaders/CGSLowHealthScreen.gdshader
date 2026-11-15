shader_type canvas_item;
render_mode unshaded, blend_disabled;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float saturation : hint_range(0.0, 1.0) = 0.60;
uniform float scan_strength : hint_range(0.0, 1.0) = 0.12;
uniform float scan_interval : hint_range(0.1, 20.0) = 9.0;
uniform float scan_duration : hint_range(0.05, 10.0) = 5.0;

void fragment() {
    vec4 scene_color = textureLod(screen_texture, SCREEN_UV, 0.0);
    float luminance = dot(scene_color.rgb, vec3(0.2126, 0.7152, 0.0722));
    vec3 color = mix(vec3(luminance), scene_color.rgb, saturation);

    float elapsed = mod(TIME, scan_interval);
    // Measure the sweep between the visible edges, with extra travel for the soft band.
    const float scan_overscan = 0.15;
    float scan_x = elapsed / scan_duration - scan_overscan;
    if (scan_x < 1.0 + scan_overscan) {
        // Move a soft band across the frame. Only its vertical stripes darken the image.
        float band = 1.0 - smoothstep(0.01, 0.14, abs(SCREEN_UV.x - scan_x));
        float stripe_phase = fract((SCREEN_UV.x - scan_x) * 90.0);
        float stripes = 1.0 - smoothstep(0.04, 0.22, abs(stripe_phase - 0.5));
        color *= 1.0 - scan_strength * band * stripes;
    }

    COLOR = vec4(color, scene_color.a);
}

shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float reveal_progress : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec4 color = textureLod(screen_texture, SCREEN_UV, 0.0);
    if (color.a > 0.0001) {
        color.rgb /= color.a;
    }
    float gray = dot(color.rgb, vec3(0.2126, 0.7152, 0.0722));
    // 从屏幕水平中线向上下展开；起点全彩，终点完整覆盖屏幕。
    float edge_softness = 0.015;
    float reveal_radius = mix(-edge_softness, 0.5 + edge_softness, reveal_progress);
    float center_distance = abs(SCREEN_UV.y - 0.5);
    float grayscale_amount = 1.0 - smoothstep(
        reveal_radius - edge_softness, reveal_radius + edge_softness, center_distance);
    color.rgb = mix(color.rgb, vec3(gray), grayscale_amount);
    COLOR *= color;
}

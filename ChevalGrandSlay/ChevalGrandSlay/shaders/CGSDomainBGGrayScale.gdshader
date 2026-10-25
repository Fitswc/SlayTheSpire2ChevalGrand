shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float saturation : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    vec4 color = textureLod(screen_texture, SCREEN_UV, 0.0);
    if (color.a > 0.0001) {
        color.rgb /= color.a;
    }
    float gray = dot(color.rgb, vec3(0.2126, 0.7152, 0.0722));
    color.rgb = mix(vec3(gray), color.rgb, saturation);
    COLOR *= color;
}

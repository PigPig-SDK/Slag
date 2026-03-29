#version 300 es
precision highp float;

out vec4 FragColor;
in vec4 color;

void main()
{
    // gl_PointCoord ranges from 0.0 to 1.0 across the point
    vec2 uv = gl_PointCoord * 2.0 - 1.0; // center at (0,0)
    float dist2 = dot(uv, uv);

    if (dist2 > 1.0 || dist2 < 0.5)
        discard; // outside the circle, discard fragment

    FragColor = color;
}
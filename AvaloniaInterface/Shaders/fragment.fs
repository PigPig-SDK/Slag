#version 300 es
precision highp float;
in vec3 normal;
out vec4 FragColor;
void main()
{
    FragColor = vec4(normal,1.0f);
}
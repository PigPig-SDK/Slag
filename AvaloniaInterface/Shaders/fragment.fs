#version 300 es
precision highp float;
in vec4 normal;
out vec4 FragColor;
void main()
{
    FragColor = (normal * 0.5 + 0.5);
}
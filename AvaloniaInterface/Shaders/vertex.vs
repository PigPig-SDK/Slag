#version 300 es
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aUV;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform mat4 projection_matrix;

out vec4 normal;

void main()
{
    normal = view_matrix * model_matrix * vec4(aNormal,1.0);
    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(aPos, 1.0);
}
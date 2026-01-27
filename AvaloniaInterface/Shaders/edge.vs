#version 300 es
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aUV;
layout (location = 3) in float metadata;

uniform vec3 camera_location;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform mat4 projection_matrix;

out vec4 color;

void main()
{
    vec4 pos = projection_matrix * view_matrix * model_matrix * vec4(aPos, 1.0);
    pos.z -= 0.0001;//Improper fix for making edges appear ontop of models (lol)
    gl_Position = pos;

    if(metadata == 1.0)
        color = vec4(1.0, 0.647, 0.0, 1.0);
    else
        color = vec4(1.0);
}
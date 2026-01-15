#version 300 es
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aUV;

uniform vec3 camera_location;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform mat4 projection_matrix;

out vec4 normal;

void main()
{
    //normal = model_matrix * vec4(aNormal,1.0);

    vec3 offset = aPos - camera_location;
    float dotProduct = dot(normalize(offset), aNormal);

    float normalContribution = 0.2;
    float normalDotContribution = 0.4;
    float colorDiff = 0.4;

    normal = (normalize(vec4(dotProduct,dotProduct,dotProduct, 1.0)) * normalDotContribution) + vec4(normalize(offset) * colorDiff, 1.0) + vec4(aNormal,1.0) * normalContribution;
    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(aPos, 1.0);
}
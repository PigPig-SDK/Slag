#version 300 es
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aUV;
layout (location = 3) in float metadata;

uniform vec3 camera_location;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform mat4 projection_matrix;
uniform mat4 env_matrix;

out vec3 normal;
out vec4 desiredColor;
out vec3 posistion;
out vec4 envSpace;
out vec2 uv;

void main()
{
    //Color base
    vec3 offset = aPos - camera_location;
    float dotProduct = dot(normalize(offset), aNormal);
    float normalDotContribution = 0.5;
    float colorDiff = 0.5;
    desiredColor = (normalize(vec4(dotProduct,dotProduct,dotProduct, 1.0)) * normalDotContribution) + vec4(normalize(offset) * colorDiff, 1.0);
    
    normal = transpose(inverse(mat3(model_matrix))) * aNormal;
    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(aPos, 1.0);
    envSpace =  env_matrix * model_matrix * vec4(aPos,1.0);
    posistion = vec3(gl_Position);
    uv = aUV;
}
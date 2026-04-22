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

uniform vec4 color;
uniform bool useColor;

out vec3 normal;
out vec4 desiredColor;
out vec4 posistion;
out vec3 posistionLocal;
out vec4 envSpace;
out vec2 uv;
out float metaDataBlend;

void main()
{
    //Color base
    if(useColor)
    {
        desiredColor = color;
    }
    else
    {
        vec3 offset = aPos - camera_location;
        float dotProduct = dot(normalize(offset), aNormal);
        float normalDotContribution = 0.5;
        float colorDiff = 0.5;
        desiredColor = (normalize(vec4(dotProduct,dotProduct,dotProduct, 1.0)) * normalDotContribution) + vec4(normalize(offset) * colorDiff, 1.0);
    }
    metaDataBlend = metadata;
    normal = transpose(inverse(mat3(model_matrix))) * aNormal;
    posistion = model_matrix * vec4(aPos, 1.0);
    gl_Position = projection_matrix * view_matrix * posistion;
    envSpace =  env_matrix * model_matrix * vec4(aPos,1.0);
    
    posistionLocal = aPos;
    uv = aUV;
}
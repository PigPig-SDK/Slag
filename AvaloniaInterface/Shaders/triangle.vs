#version 300 es
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aUV;
layout (location = 3) in float metadata;

uniform vec3 camera_location;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform mat4 projection_matrix;

out vec3 normal;
out vec4 desiredColor;
out vec3 posistion;

void main()
{
    if(metadata == 1.0)
        desiredColor = vec4(1.0, 0.647, 0.0, 1.0);
    else
    {
        vec3 offset = aPos - camera_location;
        float dotProduct = dot(normalize(offset), aNormal);

        float normalDotContribution = 0.5;
        float colorDiff = 0.5;
        desiredColor = (normalize(vec4(dotProduct,dotProduct,dotProduct, 1.0)) * normalDotContribution) + vec4(normalize(offset) * colorDiff, 1.0);
    }
    normal = aNormal;
    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(aPos, 1.0);
    posistion = vec3(gl_Position);
}
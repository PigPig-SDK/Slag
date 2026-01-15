#version 300 es
precision highp float;

uniform vec3 camera_location;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform mat4 projection_matrix;

in vec3 normal;
in vec3 posistion;

out vec4 FragColor;

void main()
{
    vec3 N = normalize(normal);

    vec3 L = normalize(camera_location - posistion);

    float diff = max(dot(N, L), 0.0);

    vec4 ambient = vec4(0.2, 0.2, 0.2, 1.0);
    vec4 lightColor = vec4(1.0, 1.0, 0.75, 1.0);

    vec4 diffuse = diff * lightColor;

    FragColor = (ambient + diffuse);
}
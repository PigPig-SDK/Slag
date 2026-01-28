#version 300 es
precision highp float;

uniform vec3 camera_location;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform mat4 projection_matrix;

in vec3 normal;
in vec3 posistion;
in vec4 desiredColor;
in float blendedMetaData;

out vec4 FragColor;

void main()
{
    float shininess = 1000.0;

    vec3 N = normalize(normal);

    vec3 L = normalize(camera_location - posistion);

    vec3 V = normalize(camera_location - posistion);

    float diff = max(dot(N, L), 0.0);
    vec4 ambient = vec4(0.7, 0.7, 0.7, 1.0);
    vec4 lightColor = vec4(1.0, 1.0, 1.0, 1.0);
    vec4 diffuse = diff * lightColor;

    
    vec3 R = reflect(-L, N);
    float spec = pow(max(dot(R, V), 0.0), shininess);
    vec4 specular = spec * lightColor;

    vec4 dColor = desiredColor;

    if(blendedMetaData > 0.999)
        dColor = vec4(1.0, 0.647, 0.0, 1.0);

    FragColor = (dColor * 0.5 + 0.5) * (ambient + diffuse + specular);
}
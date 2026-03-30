#version 300 es
precision highp float;

uniform vec3 camera_location;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform mat4 projection_matrix;
uniform sampler2D shadowMap;
uniform vec3 sunAngle;
uniform bool isFullbright;
uniform bool useTilemap;

in vec3 normal;
in vec3 posistion;
in vec4 desiredColor;
in vec4 envSpace;
in vec2 uv;

out vec4 FragColor;

float ShadowCalculation(vec4 fragPosLightSpace)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;

    if(projCoords.z > 1.0)
        return 0.0;

    float currentDepth = projCoords.z;
    float bias = 0.0005;
    float shadow = 0.0;
    vec2 texelSize = 1.0 / vec2(textureSize(shadowMap, 0));
    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r; 
            shadow += currentDepth - bias > pcfDepth ? 0.0 : 1.0;        
        }    
    }
    shadow /= 9.0;

    return shadow;
} 

void main() 
{
    if(isFullbright) {
        FragColor = desiredColor;
        return;
    }


    float shininess = 10000.0;
    vec4 baseColor = (desiredColor * 0.5 + 0.5);

    vec3 N = normalize(normal);
    vec3 L = normalize(sunAngle);  // make sure L is normalized
    vec3 V = normalize(camera_location - posistion);

    // --- Diffuse ---
    float diff = max(dot(N, L), 0.0);
    vec4 lightColor = vec4(0.972549019607843, 0.772549019607843, 0.545098039215686, 1.0);
    vec4 diffuse = diff * lightColor * baseColor; // apply base color

    // --- Specular ---
    vec3 R = reflect(-L, N);
    float spec = pow(max(dot(R, V), 0.0), shininess);
    vec4 specular = spec * lightColor;

    // --- Ambient ---
    vec4 ambient = (vec4(0.556862745098039, 0.603921568627451,0.725490196078431,2.0) / 2.0) * baseColor;

    // --- Shadow ---
    float shadow = ShadowCalculation(envSpace); // 0.0 = in shadow, 1.0 = fully lit

    // --- Combine lighting ---
    FragColor = ambient + (diffuse + specular) * shadow;
    FragColor.a = 1.0;
}
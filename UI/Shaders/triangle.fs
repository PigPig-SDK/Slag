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
uniform bool selectionHidden;

in vec3 normal;
in vec4 posistion;
in vec4 desiredColor;
in vec4 envSpace;
in vec2 uv;
in vec3 posistionLocal;
in float metaDataBlend;

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
    float blendPower = metaDataBlend * metaDataBlend;
    blendPower = blendPower * blendPower; // 4th power for smoother transition
    vec4 baseColor = (desiredColor * 0.5 + 0.5);//Pastel colors only!
    if(selectionHidden)
    {
        blendPower = 0.0;
    }
    if(!gl_FrontFacing && !useTilemap)
    {
        //Pink and black for backfacing.
        vec2 checker = floor(gl_FragCoord.xy / 20.0);
        float pattern = mod(checker.x + checker.y, 2.0);
        baseColor = mix(vec4(0.85, 0.95, 0.9, 0.2), vec4(1.0, 0.7, 0.75, 1.0), pattern);
    }

    //Lighting stage
    if(isFullbright) 
    {
        FragColor = mix(baseColor,
        vec4(1.0, 0.647, 0.0, 1.0), 
        min(blendPower,1.0));
    }
    else
    {
        float shininess = 10000.0;

        vec3 N = normalize(normal);
        vec3 L = normalize(sunAngle);  // make sure L is normalized
        vec3 V = normalize(camera_location - posistion.xyz);

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
        FragColor = mix(ambient + (diffuse + specular) * shadow,
        vec4(1.0, 0.647, 0.0, 1.0) + diffuse + ambient, 
        min(blendPower,1.0));

        FragColor.a = 1.0;
    }

    if(useTilemap) 
    {
        float gridSize = 1.0;
        float lineWidth = 0.01;
        float halfLineWidth = (lineWidth / 2.0);
        float threshold = 1.0 - lineWidth / gridSize;

        vec2 grid = fract(posistionLocal.xz / gridSize - halfLineWidth);

        bool onLine = grid.x > threshold || grid.y > threshold;

        if (!onLine) discard;

        //Fog
        float distanceToCamera = length(camera_location - posistion.xyz);
        float fogStart = 1.0;
        float fogEnd = 10.0;
        float fog = 1.0 - clamp((distanceToCamera - fogStart) / (fogEnd - fogStart), 0.0, 1.0);

        vec4 fogColor = vec4(0.1f, 0.1f, 0.1f, 0.85f);
        FragColor = mix(fogColor, FragColor, fog);

    }

}
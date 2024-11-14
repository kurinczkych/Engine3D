#version 460 core

struct Light 
{
    vec4 direction;   
    vec4 position;    
    vec4 color;       
    
    float constant;   
    float linear;
    float quadratic;
    float padding1;   
    
    vec4 ambient;     
    vec4 diffuse;     
    vec4 specular;    

    float specularPow;
    float padding2;
    float padding3;
    float padding4;

    int lightType; 
    int shadowIndex;
    int padding6;
    int padding7;

    mat4 lightSpaceSmallMatrix;
    mat4 lightSpaceMediumMatrix;
    mat4 lightSpaceLargeMatrix;
    
    float cascadeFarPlaneSmall; 
    float cascadeFarPlaneMedium;
    float cascadeFarPlaneLarge;
    float padding8;          
    
    mat4 lightSpaceTopMatrix;  
    mat4 lightSpaceBottomMatrix;
    mat4 lightSpaceLeftMatrix;
    mat4 lightSpaceRightMatrix;
    mat4 lightSpaceFrontMatrix;
    mat4 lightSpaceBackMatrix;
}; 

in vec3 gsFragPos;
in vec3 gsFragNormal;
in vec2 gsFragTexCoord;
in vec4 gsFragColor;
in mat3 gsTBN; 
in vec3 gsTangentViewDir;

uniform vec3 cameraPosition;

//TODO: Refine this array thingy, because using 64 MAX_LIGHTS, makes it exceed the maximum register size(?).
#define MAX_LIGHTS 16 //64 is max with UBO

layout(std140) uniform LightData {
    Light lights[MAX_LIGHTS];
};

uniform int actualNumOfLights;

uniform sampler2DArray smallShadowMaps;
uniform sampler2DArray mediumShadowMaps;
uniform sampler2DArray largeShadowMaps;
uniform samplerCubeArray cubeShadowMaps;

out vec4 FragColor;
uniform sampler2D textureSampler;
uniform sampler2D textureSamplerNormal;
uniform sampler2D textureSamplerHeight;
uniform sampler2D textureSamplerAO;
uniform sampler2D textureSamplerRough;
uniform sampler2D textureSamplerMetal;

uniform int useTexture;
uniform int useNormal;
uniform int useHeight;
uniform int useAO;
uniform int useRough;
uniform int useMetal;

uniform int useShading;

const float heightScale = 0.1;
const float metallnessVar = 0.04;

float DirShadowCalculation(Light light, vec3 normal)
{
    float distanceToFragment = length(gsFragPos);
    vec3 lightDir = normalize(-light.direction.xyz);

    float closestDepth = 1.0;
    float currentDepth = 0.0;

    if (distanceToFragment < light.cascadeFarPlaneSmall) 
    {
        // Transform the fragment position to light space using the selected matrix
        vec4 fragPosLightSpace = vec4(gsFragPos, 1.0) * light.lightSpaceSmallMatrix;

        // Transform to normalized device coordinates (NDC)
        vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
        projCoords = projCoords * 0.5 + 0.5; // Convert NDC to [0, 1] range

        // Check if fragment is outside the shadow map bounds
        if (projCoords.x < 0.0 || projCoords.x > 1.0 || projCoords.y < 0.0 || projCoords.y > 1.0)
        {
            return 0.0; // No shadow
        }

        // Retrieve the closest depth from the shadow map at this fragment's position
        closestDepth = texture(smallShadowMaps, vec3(projCoords.xy, light.shadowIndex)).r;

        // Current depth of the fragment from the light's perspective
        currentDepth = projCoords.z;
    }
    else if (distanceToFragment < light.cascadeFarPlaneMedium) 
    {
        vec4 fragPosLightSpace = vec4(gsFragPos, 1.0) * light.lightSpaceMediumMatrix;

        vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
        projCoords = projCoords * 0.5 + 0.5;

        if (projCoords.x < 0.0 || projCoords.x > 1.0 || projCoords.y < 0.0 || projCoords.y > 1.0)
        {
            return 0.0; // No shadow
        }

        closestDepth = texture(mediumShadowMaps, vec3(projCoords.xy, light.shadowIndex)).r;

        currentDepth = projCoords.z;
    }
    else 
    {
        vec4 fragPosLightSpace = vec4(gsFragPos, 1.0) * light.lightSpaceLargeMatrix;

        vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
        projCoords = projCoords * 0.5 + 0.5;

        if (projCoords.x < 0.0 || projCoords.x > 1.0 || projCoords.y < 0.0 || projCoords.y > 1.0)
        {
            return 0.0; // No shadow
        }

        closestDepth = texture(largeShadowMaps, vec3(projCoords.xy, light.shadowIndex)).r;

        currentDepth = projCoords.z;
    }

    // Calculate bias to reduce shadow artifacts
    float slopeScaleFactor = 0.01;
    float constantBias = 0.0005;
    float bias = max(slopeScaleFactor * (1.0 - dot(normal, lightDir)), constantBias);

    // Perform shadow comparison
    float shadow = (currentDepth - bias > closestDepth) ? 1.0 : 0.0;

    return shadow;
}

float PointShadowCalculation(Light light, vec3 normal)
{
    vec3 lightDir = gsFragPos - vec3(light.position);

    // Sample the cube shadow map directly
    float closestDepth = texture(cubeShadowMaps, vec4(normalize(lightDir), light.shadowIndex)).r;
    float currentDepth = length(lightDir);
    
    // Calculate bias to avoid shadow artifacts
    float slopeScaleFactor = 0.01;
    float constantBias = 0.0005;
    float bias = max(slopeScaleFactor * (1.0 - dot(normal, normalize(lightDir))), constantBias);

    // Perform shadow comparison
    float shadow = (currentDepth - bias > closestDepth) ? 1.0 : 0.0;
    return shadow;
}

vec3 CalcPointLight(Light light, vec3 normal, vec3 viewDir, float metalness)
{
    vec3 lightDir = normalize(light.position.xyz - gsFragPos);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    // Diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    // Specular shading
    float spec = pow(max(dot(normal, halfwayDir), 0.0), light.specularPow);

    if (useRough == 1)
    {
        float roughness = texture(textureSamplerRough, gsFragTexCoord).r;
        float m = roughness * roughness;
        spec = pow(max(dot(normal, halfwayDir), 0.0), light.specularPow / m);
    }

    // Attenuation
    float distance = length(light.position.xyz - gsFragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + 
                               light.quadratic * (distance * distance));    

    // Ambient occlusion
    vec3 ambient = light.ambient.xyz * light.diffuse.xyz;
    if (useAO == 1)
    {
        float ao = texture(textureSamplerAO, gsFragTexCoord).r;
        ambient *= ao;
    }

    // Calculate diffuse and specular terms
    vec3 diffuse = light.diffuse.xyz * diff * light.diffuse.xyz;
    vec3 specular = light.specular.xyz * spec * light.specular.xyz;

    if (useMetal == 1)
    {
        vec3 k_s = mix(vec3(metallnessVar), vec3(1.0), metalness); // Reflectance at normal incidence
        vec3 k_d = k_s;

        diffuse *= k_d;
        specular *= k_s;
    }

    // Apply attenuation to ambient, diffuse, and specular contributions
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;

    // Final color calculation, ensuring that contributions do not affect the back face
    float shadow = PointShadowCalculation(light, normal); 
    return (ambient + diffuse * (1.0 - shadow * 0.5) + specular * (1.0 - shadow)) * light.color.xyz;
//    return (ambient + diffuse + specular) * light.color.xyz;
} 

vec3 CalcDirLight(Light light, vec3 normal, vec3 viewDir, float metalness)
{
    vec3 lightDir = normalize(-light.direction.xyz);

    // Diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    // Specular shading
    float spec;
    vec3 reflectDir = reflect(-lightDir, normal);
    if(useRough == 1)
    {
        float roughness = texture(textureSamplerRough, gsFragTexCoord).r;
        float m = roughness * roughness;
        spec = pow(max(dot(viewDir, reflectDir), 0.0), light.specularPow / m);
    }
    else
    {
        spec = pow(max(dot(viewDir, reflectDir), 0.0), light.specularPow / 1);
    }

    // Ambient lighting
    vec3 ambient = light.ambient.xyz * light.diffuse.xyz;
    if (useAO == 1)
    {
        float ao = texture(textureSamplerAO, gsFragTexCoord).r;
        ambient *= ao;
    }

    // Diffuse and Specular calculation
    vec3 diffuse = light.diffuse.xyz * diff;
    vec3 specular = light.specular.xyz * spec;

    // Metalness adjustment (using Fresnel-Schlick approximation)
    if (useMetal == 1)
    {
        vec3 F0 = mix(vec3(0.04), vec3(metallnessVar), metalness); // Non-metal F0 is around 0.04
        vec3 k_s = F0;                                            // Metals have high reflectance
        vec3 k_d = vec3(1.0) - k_s;                               // Non-metals have diffuse, metals don't

        diffuse *= k_d;
        specular *= k_s;
    }

    // Shadow calculation (attenuate specular more than diffuse to retain depth)
    float shadow = DirShadowCalculation(light, normal); 
    return (ambient + diffuse * (1.0 - shadow * 0.5) + specular * (1.0 - shadow)) * light.color.xyz;
}

vec2 ParallaxMapping(vec2 texCoords, vec3 viewDir) {
    float height = texture(textureSamplerHeight, texCoords).r - 0.5;
    return texCoords + height * heightScale * viewDir.xy;
}

void main()
{
    vec2 parallaxTexCoords = vec2(0,0);
    vec3 tex = texture(textureSamplerNormal, gsFragTexCoord).rgb;
    
    float metalness = 1;
    if(useMetal == 1)
    {
        metalness = texture(textureSamplerMetal, gsFragTexCoord).r;
    }
    if(useHeight == 1)
    {
        parallaxTexCoords = ParallaxMapping(gsFragTexCoord, gsTangentViewDir);
        tex = texture(textureSamplerNormal, parallaxTexCoords).rgb;

        if(useMetal == 1)
        {
            metalness = texture(textureSamplerMetal, parallaxTexCoords).r;
        }
    }

    vec3 viewDir = normalize(cameraPosition - gsFragPos);

    vec3 sampledNormal = gsFragNormal;
    vec3 normalFromMap = gsFragNormal;

    if(useNormal == 1)
    {
        sampledNormal = normalize(2.0 * tex - 1.0);
        normalFromMap = normalize(gsTBN * sampledNormal);
    }

    vec3 result = vec3(1);
    if(useShading == 1)
    {
        result = vec3(0);

        for(int i = 0; i < actualNumOfLights; i++)
        {
            if(lights[i].lightType == 0)
            {
                result += CalcPointLight(lights[i], normalFromMap, viewDir, metalness);
            }
            else if(lights[i].lightType == 1)
            {
                result += CalcDirLight(lights[i], normalFromMap, viewDir, metalness);
            }
        }
    }

    FragColor = vec4(result, 1.0) * gsFragColor;
    if(useTexture == 1)
    {
        FragColor = texture(textureSampler, gsFragTexCoord) * vec4(result, 1.0) * gsFragColor;
        if(useHeight == 1)
        {
            FragColor = texture(textureSampler, parallaxTexCoords) * vec4(result, 1.0) * gsFragColor;
        }
    }
}


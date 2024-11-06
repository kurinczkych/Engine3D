#version 330 core

struct Light 
{
    vec3 direction;
    vec3 position;
    vec3 color;
    
    float constant;
    float linear;
    float quadratic;  

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float specularPow;

    int lightType;
};

in vec3 gsFragPos;
in vec3 gsFragNormal;
in vec2 gsFragTexCoord;
in vec4 gsFragColor;
in mat3 gsTBN; 
in vec3 gsTangentViewDir;

uniform vec3 cameraPosition;

#define MAX_LIGHTS 64
uniform Light lights[MAX_LIGHTS];
uniform int actualNumOfLights;

out vec4 FragColor;
uniform sampler2D textureSampler;
uniform sampler2D textureSamplerNormal;
uniform sampler2D textureSamplerHeight;
uniform sampler2D textureSamplerAO;
uniform sampler2D textureSamplerRough;
uniform sampler2D textureSamplerMetal;

uniform sampler2D shadowMapSmall;
uniform sampler2D shadowMapMedium;
uniform sampler2D shadowMapLarge;
uniform mat4 lightSpaceSmallMatrix;
uniform mat4 lightSpaceMediumMatrix;
uniform mat4 lightSpaceLargeMatrix;
uniform float cascadeFarPlaneSmall;
uniform float cascadeFarPlaneMedium;
uniform float cascadeFarPlaneLarge;

uniform int useTexture;
uniform int useNormal;
uniform int useHeight;
uniform int useAO;
uniform int useRough;
uniform int useMetal;

uniform int useShading;

const float heightScale = 0.1;
const float metallnessVar = 0.04;

float ShadowCalculation(vec3 lightDir, vec3 normal)
{
    float distanceToFragment = length(gsFragPos);

    float closestDepth = 0;
    float currentDepth = 0;

    if (distanceToFragment < cascadeFarPlaneSmall) 
    {
        // Transform the fragment position to light space using the selected matrix
        vec4 fragPosLightSpace = vec4(gsFragPos, 1.0) * lightSpaceSmallMatrix;

        // Transform to normalized device coordinates (NDC)
        vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
        projCoords = projCoords * 0.5 + 0.5; // Convert NDC to [0, 1] range

        // Check if fragment is outside the shadow map bounds
        if (projCoords.x < 0.0 || projCoords.x > 1.0 || projCoords.y < 0.0 || projCoords.y > 1.0)
        {
            return 0.0; // No shadow
        }

        // Retrieve the closest depth from the shadow map at this fragment's position
        closestDepth = texture(shadowMapSmall, projCoords.xy).r;

        // Current depth of the fragment from the light's perspective
        currentDepth = projCoords.z;
    }
    else if (distanceToFragment < cascadeFarPlaneMedium) 
    {
        // Transform the fragment position to light space using the selected matrix
        vec4 fragPosLightSpace = vec4(gsFragPos, 1.0) * lightSpaceMediumMatrix;

        // Transform to normalized device coordinates (NDC)
        vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
        projCoords = projCoords * 0.5 + 0.5; // Convert NDC to [0, 1] range

        // Check if fragment is outside the shadow map bounds
        if (projCoords.x < 0.0 || projCoords.x > 1.0 || projCoords.y < 0.0 || projCoords.y > 1.0)
        {
            return 0.0; // No shadow
        }

        // Retrieve the closest depth from the shadow map at this fragment's position
        closestDepth = texture(shadowMapMedium, projCoords.xy).r;

        // Current depth of the fragment from the light's perspective
        currentDepth = projCoords.z;
    }
    else 
    {
        // Transform the fragment position to light space using the selected matrix
        vec4 fragPosLightSpace = vec4(gsFragPos, 1.0) * lightSpaceLargeMatrix;

        // Transform to normalized device coordinates (NDC)
        vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
        projCoords = projCoords * 0.5 + 0.5; // Convert NDC to [0, 1] range

        // Check if fragment is outside the shadow map bounds
        if (projCoords.x < 0.0 || projCoords.x > 1.0 || projCoords.y < 0.0 || projCoords.y > 1.0)
        {
            return 0.0; // No shadow
        }

        // Retrieve the closest depth from the shadow map at this fragment's position
        closestDepth = texture(shadowMapLarge, projCoords.xy).r;

        // Current depth of the fragment from the light's perspective
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

vec3 CalcPointLight(Light light, vec3 normal, vec3 fragPos, vec3 viewDir, float metalness)
{
    vec3 lightDir = normalize(light.position - fragPos);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    // specular shading
    float spec = pow(max(dot(normal, halfwayDir), 0.0), light.specularPow);

    if(useRough == 1)
    {
        float roughness = texture(textureSamplerRough, gsFragTexCoord).r;
        float m = roughness * roughness;
        spec = pow(max(dot(normal, halfwayDir), 0.0), light.specularPow / m);
    }

    // attenuation
    float distance    = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + 
  			     light.quadratic * (distance * distance));    

    // combine results
    float ao = texture(textureSamplerAO, gsFragTexCoord).r;
    vec3 ambient  = light.ambient * light.diffuse;
    if(useAO == 1)
    {
        ambient  = ambient * ao;
    }

    vec3 diffuse  = light.diffuse  * diff * light.diffuse;
    vec3 specular = light.specular * spec * light.specular;

    if(useMetal == 1)
    {
        vec3 k_s = mix(vec3(metallnessVar), vec3(1.0), metalness); // Reflectance at normal incidence, can be tweaked.
        vec3 k_d = k_s;

        diffuse = diffuse * k_d;
        specular = specular * k_s;
    }

    ambient  *= attenuation;
    diffuse  *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular) * light.color;
} 

vec3 CalcDirLight(Light light, vec3 normal, vec3 viewDir, float metalness)
{
    vec3 lightDir = normalize(-light.direction);

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
    vec3 ambient = light.ambient * light.diffuse;
    if (useAO == 1)
    {
        float ao = texture(textureSamplerAO, gsFragTexCoord).r;
        ambient *= ao;
    }

    // Diffuse and Specular calculation
    vec3 diffuse = light.diffuse * diff;
    vec3 specular = light.specular * spec;

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
    float shadow = ShadowCalculation(lightDir, normal); 
    return (ambient + diffuse * (1.0 - shadow * 0.5) + specular * (1.0 - shadow)) * light.color;
//    return (ambient + diffuse + specular) * light.color;
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
                result += CalcPointLight(lights[i], normalFromMap, gsFragPos, viewDir, metalness);
            }
            else if(lights[i].lightType == 1)
            {
                // Calculate the light-space position for shadow mapping
                result += CalcDirLight(lights[i], normalFromMap, viewDir, metalness);
                //result += CalcDirLight(lights[i], normalFromMap, viewDir, metalness);
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


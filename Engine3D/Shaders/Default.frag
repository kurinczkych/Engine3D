#version 330 core

struct PointLight {    
    vec3 position;
    vec3 color;
    
    float constant;
    float linear;
    float quadratic;  

    vec3 ambient;
    vec3 diffuse;

    float specularPow;
    vec3 specular;
};

struct DirLight {
    vec3 direction;
  
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float specularPow;
};  

in vec3 gsFragPos;
in vec3 gsFragNormal;
in vec2 gsFragTexCoord;
in vec4 gsFragColor;
in mat3 gsTBN; 
in vec3 gsTangentViewDir;

uniform vec3 cameraPosition;

#define MAX_LIGHTS 64
uniform PointLight pointLights[MAX_LIGHTS];
uniform int actualNumOfLights;

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

const float heightScale = 0.1;
const float metallnessVar = 0.04;

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, float metalness)
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
    return (ambient + diffuse + specular);
} 

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir, float metalness)
{
    vec3 lightDir = normalize(-light.direction);

    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    // specular shading
    float roughness = texture(textureSamplerRough, gsFragTexCoord).r;
    float m = roughness * roughness;
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), light.specularPow);

    if(useRough == 1)
    {
        float roughness = texture(textureSamplerRough, gsFragTexCoord).r;
        float m = roughness * roughness;
        spec = pow(max(dot(viewDir, reflectDir), 0.0), light.specularPow / m);
    }

    // combine results
    vec3 ambient  = light.ambient * light.diffuse;
    if(useAO == 1)
    {
        float ao = texture(textureSamplerAO, gsFragTexCoord).r;
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

    return (ambient + diffuse + specular);
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

    DirLight dirLight;
    dirLight.direction = vec3(0,-1,0);
    //dirLight.direction = normalize(vec3(0.0, -1.0, -1.0));
    dirLight.ambient = vec3(0.1,0.1,0.1);
    dirLight.diffuse = vec3(1.0,1.0,1.0);
    dirLight.specular = vec3(1.0,1.0,1.0);
    dirLight.specularPow = 2;

    vec3 result = vec3(0,0,0);

    // phase 1: Directional lighting
    result = CalcDirLight(dirLight, normalFromMap, viewDir, metalness);

    // phase 2: Point lights
    for(int i = 0; i < actualNumOfLights; i++)
        result += CalcPointLight(pointLights[i], normalFromMap, gsFragPos, viewDir, metalness);

    FragColor = vec4(result, 1.0) * gsFragColor;
    if(useTexture == 1)
    {
        FragColor = texture(textureSampler, gsFragTexCoord) * vec4(result, 1.0) * gsFragColor;
        if(useHeight == 1)
        {
            FragColor = texture(textureSampler, parallaxTexCoords) * vec4(result, 1.0) * gsFragColor;
        }
    }

//    FragColor = fragColor;
}


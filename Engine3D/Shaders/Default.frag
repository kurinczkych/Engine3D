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

in vec3 fragPos;
in vec3 normal;
in vec2 fragTexCoord;
in vec4 fragColor;
in mat3 TBN; 
in vec3 TangentViewDir;

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

uniform int useNormal;
uniform int useHeight;
uniform int useAO;
uniform int useRough;

const float heightScale = 0.1; // Adjust this value to your needs

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    // specular shading
    float spec = pow(max(dot(normal, halfwayDir), 0.0), light.specularPow);

    if(useRough == 1)
    {
        float roughness = texture(textureSamplerRough, fragTexCoord).r;
        float m = roughness * roughness;
        spec = pow(max(dot(normal, halfwayDir), 0.0), light.specularPow / m);
    }

    // attenuation
    float distance    = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + 
  			     light.quadratic * (distance * distance));    

    // combine results
    float ao = texture(textureSamplerAO, fragTexCoord).r;
    vec3 ambient  = light.ambient * light.diffuse;
    if(useAO == 1)
    {
        ambient  = ambient * ao;
    }

    vec3 diffuse  = light.diffuse  * diff * light.diffuse;
    vec3 specular = light.specular * spec * light.specular;
    ambient  *= attenuation;
    diffuse  *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular);
} 

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir)
{
    vec3 lightDir = normalize(-light.direction);

    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    // specular shading
    float roughness = texture(textureSamplerRough, fragTexCoord).r;
    float m = roughness * roughness;
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), light.specularPow);

    if(useRough == 1)
    {
        float roughness = texture(textureSamplerRough, fragTexCoord).r;
        float m = roughness * roughness;
        spec = pow(max(dot(viewDir, reflectDir), 0.0), light.specularPow / m);
    }

    // combine results
    float ao = texture(textureSamplerAO, fragTexCoord).r;
    vec3 ambient  = light.ambient * light.diffuse;
    if(useAO == 1)
    {
        ambient  = ambient * ao;
    }

    vec3 diffuse  = light.diffuse  * diff * light.diffuse;
    vec3 specular = light.specular * spec * light.specular;

    return (ambient + diffuse + specular);
}  

vec2 ParallaxMapping(vec2 texCoords, vec3 viewDir) {
    float height = texture(textureSamplerHeight, texCoords).r - 0.5;  // Assuming the height map is centered around 0.5
    return texCoords + height * heightScale * viewDir.xy;
}

//struct DirLight {
//    vec3 direction;
//  
//    vec3 ambient;
//    vec3 diffuse;
//    vec3 specular;
//    float specularPow;
//};  

void main()
{
    vec2 parallaxTexCoords = vec2(0,0);
    vec3 tex = texture(textureSamplerNormal, fragTexCoord).rgb;
    if(useHeight == 1)
    {
        parallaxTexCoords = ParallaxMapping(fragTexCoord, TangentViewDir);
        tex = texture(textureSamplerNormal, parallaxTexCoords).rgb;
    }

    vec3 viewDir = normalize(cameraPosition - fragPos);

    vec3 sampledNormal = normal;
    vec3 normalFromMap = normal;

    if(useNormal == 1)
    {
        sampledNormal = normalize(2.0 * tex - 1.0);
        normalFromMap = normalize(TBN * sampledNormal);
    }

    DirLight dirLight;
    dirLight.direction = vec3(0,-1,0);
//    dirLight.direction = normalize(vec3(0.0, -1.0, -1.0));
    dirLight.ambient = vec3(0.1,0.1,0.1);
    dirLight.diffuse = vec3(1.0,1.0,1.0);
    dirLight.specular = vec3(1.0,1.0,1.0);
    dirLight.specularPow = 2;

    vec3 result = vec3(0,0,0);

    // phase 1: Directional lighting
    result = CalcDirLight(dirLight, normalFromMap, viewDir);

    // phase 2: Point lights
    for(int i = 0; i < actualNumOfLights; i++)
        result += CalcPointLight(pointLights[i], normalFromMap, fragPos, viewDir);

    FragColor = texture(textureSampler, fragTexCoord) * vec4(result, 1.0) * fragColor;
    if(useHeight == 1)
    {
        FragColor = texture(textureSampler, parallaxTexCoords) * vec4(result, 1.0) * fragColor;
    }
//    FragColor = fragColor;
//    FragColor = vec4(result, 1.0);
}


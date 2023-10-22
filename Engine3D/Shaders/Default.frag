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

uniform vec3 cameraPosition;

#define MAX_LIGHTS 64
uniform PointLight pointLights[MAX_LIGHTS];
uniform int actualNumOfLights;

out vec4 FragColor;
uniform sampler2D textureSampler;


vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);
    vec3 halfwayDir = normalize(lightDir + viewDir);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
//    vec3 reflectDir = reflect(-lightDir, normal);
//    float spec = pow(max(dot(viewDir, reflectDir), 0.0), light.specularPow);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), light.specularPow);
    // attenuation
    float distance    = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + 
  			     light.quadratic * (distance * distance));    
    // combine results


    vec3 ambient  = light.ambient  * light.diffuse;
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
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), light.specularPow);
    // combine results
    vec3 ambient  = light.ambient  * light.diffuse;
    vec3 diffuse  = light.diffuse  * diff * light.diffuse;
    vec3 specular = light.specular * spec * light.specular;

    vec3 lighting = (ambient + diffuse + specular);

    return lighting;
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
    vec3 viewDir = normalize(cameraPosition - fragPos);

    DirLight dirLight;
    dirLight.direction = vec3(0,-1,0);
    dirLight.ambient = vec3(0.1,0.1,0.1);
    dirLight.diffuse = vec3(1.0,1.0,1.0);
    dirLight.specular = vec3(1.0,1.0,1.0);
    dirLight.specularPow = 2;

    vec3 result = vec3(0,0,0);

    // phase 1: Directional lighting
    result = CalcDirLight(dirLight, normal, viewDir);

    // phase 2: Point lights
    for(int i = 0; i < actualNumOfLights; i++)
        result += CalcPointLight(pointLights[i], normal, fragPos, viewDir);

    FragColor = texture(textureSampler, fragTexCoord) * vec4(result, 1.0) * fragColor;
//    FragColor = fragColor;
//    FragColor = vec4(result, 1.0);
}


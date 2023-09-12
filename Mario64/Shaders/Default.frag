#version 330 core

in vec4 fragColor;

in vec2 fragTexCoord;

out vec4 FragColor;
uniform sampler2D textureSampler;

void main()
{
//    vec2 correctedTexCoord = fragTexCoord * fragDepthW * fragTexCoordW;
//    vec2 correctedTexCoord = fragTexCoord;
    FragColor = fragColor * texture(textureSampler, fragTexCoord);;
//    FragColor = fragColor * texture(textureSampler, fragTexCoord);
}
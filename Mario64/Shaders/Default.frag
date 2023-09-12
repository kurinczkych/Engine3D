#version 330 core

in vec4 fragColor;

in vec2 fragTexCoord;
//in float rhw;

out vec4 FragColor;
uniform sampler2D textureSampler;

void main()
{
//    vec2 correctedUV = fragTexCoord * rhw;
    FragColor = fragColor * texture(textureSampler, fragTexCoord);
//    FragColor = fragColor;
//    FragColor = fragColor;
}
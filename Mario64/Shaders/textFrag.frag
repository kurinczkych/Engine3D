#version 330 core

in vec4 fragColor;
in vec2 fragTexCoord;

out vec4 FragColor;
uniform sampler2D textureSampler;

void main()
{
    FragColor = texture(textureSampler, fragTexCoord) * fragColor;
}


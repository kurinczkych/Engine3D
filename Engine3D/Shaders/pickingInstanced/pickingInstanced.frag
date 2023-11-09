#version 330 core

uniform uint objectIndex;

flat in uint instIndex;

out uvec3 FragColor;

void main()
{
    FragColor = uvec3(objectIndex, 0, instIndex); 
}


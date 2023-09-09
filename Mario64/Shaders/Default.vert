#version 330 core

layout (location = 0) in vec3 aPosition; // vertex coordinates
layout(location = 1) in vec4 inColor;

out vec4 fragColor;

void main()
{
	gl_Position = vec4(aPosition, 1.0); // coordinates
	fragColor = inColor;
}
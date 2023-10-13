#version 330 core

layout (location = 0) in vec4 inPosition;
layout (location = 1) in vec4 inColor;

out vec4 fragColor;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

void main()
{
	gl_Position = inPosition * modelMatrix * viewMatrix * projectionMatrix;
	fragColor = inColor;
}
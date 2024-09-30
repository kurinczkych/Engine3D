#version 430 core

layout(location = 0) in vec3 inPosition;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

out vec3 fragPos;

void main()
{
    fragPos = vec3(vec4(inPosition, 1.0) * modelMatrix);
    gl_Position = vec4(inPosition, 1.0) * modelMatrix * viewMatrix * projectionMatrix;
}

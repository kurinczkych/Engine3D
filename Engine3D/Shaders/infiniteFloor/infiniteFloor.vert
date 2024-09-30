#version 430 core

layout(location = 0) in vec3 inPosition;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

void main()
{
//    gl_Position = projectionMatrix * viewMatrix * modelMatrix * vec4(inPosition, 1.0);
    gl_Position = vec4(inPosition, 1.0) * modelMatrix * viewMatrix * projectionMatrix; //from default.vert
//    gl_Position = vec4(inPosition, 1.0);
}

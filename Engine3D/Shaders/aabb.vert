#version 330 core

layout(location = 0) in vec3 aPos;  // Vertex position

uniform mat4 modelMatrix;       // Model matrix for the AABB
uniform mat4 viewMatrix;        // View matrix
uniform mat4 projectionMatrix;  // Projection matrix

void main()
{
    gl_Position = projectionMatrix * viewMatrix * modelMatrix * vec4(aPos, 1.0);
}
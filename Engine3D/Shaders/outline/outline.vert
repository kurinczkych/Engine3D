#version 330 core

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec3 inNormal;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

void main()
{
    float outlineWidth = 100; // This is a relative value, not pixels.

    // Calculate world position of the vertex
    vec4 worldPosition = modelMatrix * vec4(inPosition, 1.0);

    // Calculate view space position of the vertex for the outline
    vec4 viewPosition = viewMatrix * worldPosition;

    // Calculate the distance from the camera
    float distance = length(viewPosition.xyz);

    // Adjust outline width based on the distance
    // The outlineWidth is divided by distance to convert a world space width to a clip space width.
    float scaledOutlineWidth = outlineWidth / distance;
//
//    // Enlarge the vertex position along the normal by the scaled outline width
//    vec3 enlargedPosition = inPosition + (inNormal * scaledOutlineWidth);
//
//    // Calculate the final transformed vertex position
//    gl_Position = vec4(enlargedPosition, 1.0) * modelMatrix * viewMatrix * projectionMatrix;

	gl_Position = vec4(inPosition + inNormal * 2, 1.0) * modelMatrix * viewMatrix * projectionMatrix;
}
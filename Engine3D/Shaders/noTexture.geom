#version 330 core

layout(lines) in; // Input primitive is lines
layout(triangle_strip, max_vertices = 4) out; // We will output a triangle strip forming a quad

in vec4 gsFragColor[];

out vec4 fragColor;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;

void main() {

    float lineWidth = 0.1;
    vec4 clipSpaceStartPosition = gl_in[0].gl_Position;
    vec4 clipSpaceEndPosition = gl_in[1].gl_Position;

    // Calculate the direction in clip space
    vec3 clipSpaceDir = normalize(clipSpaceEndPosition.xyz - clipSpaceStartPosition.xyz);
    vec3 screenSpacePerp = vec3(-clipSpaceDir.y, clipSpaceDir.x, 0.0); // Rotate 90 degrees in screen space

    // Expand the line in screen space
    vec4 offset = vec4(screenSpacePerp.xy * lineWidth, 0.0, 0.0);

    // Create the quad vertices
    gl_Position = clipSpaceStartPosition + offset;
    fragColor = gsFragColor[0]; // Pass the color to the fragment shader
    EmitVertex();

    gl_Position = clipSpaceStartPosition - offset;
    fragColor = gsFragColor[0];
    EmitVertex();

    gl_Position = clipSpaceEndPosition + offset;
    fragColor = gsFragColor[1]; // Interpolate color for the second vertex
    EmitVertex();

    gl_Position = clipSpaceEndPosition - offset;
    fragColor = gsFragColor[1];
    EmitVertex();

    EndPrimitive();
}
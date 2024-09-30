#version 430 core

out vec4 fragColor;

uniform vec3 cameraPos; // Camera world position
uniform float gridScale = 10.0; // Grid scale
uniform float lineThickness = 0.01; // Thickness of the grid lines
uniform vec3 gridColor = vec3(1.0); // Color of the grid lines
uniform vec3 backgroundColor = vec3(0.0); // Background color
uniform float gridFadeDistance = 50.0; // Distance where grid starts to fade

float gridPattern(float coord) {
    float line = abs(fract(coord) - 0.5);
    return smoothstep(lineThickness, 0.0, line);
}

void main() {
    vec2 gridUV = cameraPos.xz / gridScale;
    float lineX = gridPattern(gridUV.x);
    float lineY = gridPattern(gridUV.y);
    float grid = max(lineX, lineY);

    float distanceFromCamera = length(cameraPos);
    float fadeFactor = clamp(1.0 - (distanceFromCamera / gridFadeDistance), 0.0, 1.0);
    
    vec3 finalColor = mix(backgroundColor, gridColor, grid * fadeFactor);
    fragColor = vec4(finalColor, 1.0);

//    fragColor = vec4(1.0, 0.0, 0.0, 1.0); // Pure red color with full opacity
}

#version 430 core

out vec4 fragColor;
in vec3 fragPos;

uniform vec3 cameraPos;
uniform vec3 bgColor;
uniform vec3 lineColor;

void main()
{
    float gridSpacing = 25.0;
    float lineWidth = 1.0;
    float maxDistanceColor = 1000.0;
    float maxDistanceLine = 100.0;

    float distance = length(cameraPos - fragPos);

    // Dynamically increase line width based on the distance from the camera
    float adjustedLineWidth = lineWidth + (distance / maxDistanceLine) * lineWidth;
    
    // Ensure that the line width doesn't get too large at far distances
    adjustedLineWidth = clamp(adjustedLineWidth, lineWidth, lineWidth * 10.0);

    // Use the X and Z coordinates from FragPos for a floor grid pattern
    float modX = mod(fragPos.x, gridSpacing);
    float modZ = mod(fragPos.z, gridSpacing);

    // Check if the fragment is close to a grid line on either axis
    if (modX < adjustedLineWidth || modZ < adjustedLineWidth)
    {
        // Calculate the blending factor for the color transition based on distance
        float colorFactor = clamp(distance / maxDistanceColor, 0.0, 1.0);
        vec3 gridColor = mix(lineColor, bgColor, colorFactor);

        fragColor = vec4(gridColor, 1.0);

    }
    else
    {
        discard;
    }
}
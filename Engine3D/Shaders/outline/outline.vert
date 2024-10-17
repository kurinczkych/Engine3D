#version 330 core

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec3 inNormal;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;
uniform vec3 cameraPos;

uniform mat4 _scaleMatrix;
uniform mat4 _rotMatrix;

mat4 GetTranslationMatrix(mat4 mMatrix)
{
    vec3 translation = vec3(mMatrix[0][3], mMatrix[1][3], mMatrix[2][3]);

    // Construct a translation-only matrix
    mat4 translationMatrix = mat4(
        vec4(1.0, 0.0, 0.0, translation.x),
        vec4(0.0, 1.0, 0.0, translation.y),
        vec4(0.0, 0.0, 1.0, translation.z),
        vec4(0.0, 0.0, 0.0, 1.0) // Set the translation component
    );

    return translationMatrix;
}

void main()
{
//	mat4 transMatrix = GetTranslationMatrix(modelMatrix);
//
//    vec4 positionedVertex = vec4(inPosition, 1.0) * _scaleMatrix * _rotMatrix * transMatrix;
//
////    float distance = length(abs(positionedVertex.xyz-cameraPos));
////    float outlineWidth = (9.0/580.0) * distance + (10.0/29.0);
//    float outlineWidth = 2;
//
//    vec4 rotatedNormal = vec4(inNormal * outlineWidth, 1.0) * _rotMatrix;
//    vec4 final = positionedVertex + rotatedNormal;

    vec4 positionedVertex = vec4(inPosition, 1.0) * modelMatrix;
    vec4 final = positionedVertex + vec4(inNormal, 1.0);

    gl_Position = vec4(final.xyz,1.0) * viewMatrix * projectionMatrix;
}
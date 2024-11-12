#version 330 core

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec3 inNormal;
layout(location = 2) in vec3 instPosition;
layout(location = 3) in vec4 instRotation;
layout(location = 4) in vec3 instScale;
layout(location = 5) in vec4 instColor;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform mat4 _scaleMatrix;
uniform mat4 _rotMatrix;

mat4 constructRotationMatrix(vec4 quat) {
    float x = quat.x;
    float y = quat.y;
    float z = quat.z;
    float w = quat.w;

    return mat4(
        1 - 2 * y * y - 2 * z * z,     2 * x * y + 2 * z * w,       2 * x * z - 2 * y * w,       0,
        2 * x * y - 2 * z * w,         1 - 2 * x * x - 2 * z * z,   2 * y * z + 2 * x * w,       0,
        2 * x * z + 2 * y * w,         2 * y * z - 2 * x * w,       1 - 2 * x * x - 2 * y * y,   0,
        0,                             0,                           0,                           1
    );
}

mat4 GetTranslationMatrixVec3(vec3 translation)
{
    mat4 translationMatrix = mat4(
        vec4(1.0, 0.0, 0.0, translation.x),
        vec4(0.0, 1.0, 0.0, translation.y),
        vec4(0.0, 0.0, 1.0, translation.z),
        vec4(0.0, 0.0, 0.0, 1.0)
    );

    return translationMatrix;
}


void main() {
    float outlineWidth = 1.0;

	mat4 instRotMatrix = constructRotationMatrix(instRotation);

    mat4 instScaleMatrix = mat4(
        vec4(instScale.x, 0.0, 0.0, 0.0),
        vec4(0.0, instScale.y, 0.0, 0.0),
        vec4(0.0, 0.0, instScale.z, 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );

    mat4 instTransMatrix = GetTranslationMatrixVec3(instPosition);

    vec4 pos = vec4(inPosition,1.0) * instScaleMatrix;
    vec4 norm = vec4(inNormal,0.0) * outlineWidth;
    vec4 final = pos + norm;
    final = final * instRotMatrix * instTransMatrix * modelMatrix;
    final.w = 1.0;


    gl_Position = final * viewMatrix * projectionMatrix;
}
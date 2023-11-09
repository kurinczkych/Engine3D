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

mat3 convertQuaternionToMat3(vec4 q) {
    // Convert quaternion to 3x3 rotation matrix
    float qx2 = q.x * q.x;
    float qy2 = q.y * q.y;
    float qz2 = q.z * q.z;

    float qxqy = q.x * q.y;
    float qxqz = q.x * q.z;
    float qxqw = q.x * q.w;

    float qyqz = q.y * q.z;
    float qyqw = q.y * q.w;
    
    float qzqw = q.z * q.w;

    mat3 m;
    m[0][0] = 1.0 - 2.0 * (qy2 + qz2);
    m[0][1] = 2.0 * (qxqy + qzqw);
    m[0][2] = 2.0 * (qxqz - qyqw);

    m[1][0] = 2.0 * (qxqy - qzqw);
    m[1][1] = 1.0 - 2.0 * (qx2 + qz2);
    m[1][2] = 2.0 * (qyqz + qxqw);

    m[2][0] = 2.0 * (qxqz + qyqw);
    m[2][1] = 2.0 * (qyqz - qxqw);
    m[2][2] = 1.0 - 2.0 * (qx2 + qy2);

    return m;
}

void main()
{
    float outlineWidth = 1;

    mat4 transMatrix = GetTranslationMatrix(modelMatrix);

    mat3 rotationMatrix = convertQuaternionToMat3(instRotation);
    mat4 rotationMatrix4 = mat4(
        vec4(rotationMatrix[0], 0),
        vec4(rotationMatrix[1], 0),
        vec4(rotationMatrix[2], 0),
        vec4(0, 0, 0, 1)  // This makes it an affine transformation matrix
    );
    mat4 scaleMatrix = mat4(
        vec4(instScale.x, 0.0, 0.0, 0.0),
        vec4(0.0, instScale.y, 0.0, 0.0),
        vec4(0.0, 0.0, instScale.z, 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );

//    vec4 rotatedVertex = vec4(rotationMatrix * inPosition, 1.0);
    vec4 rotatedVertex = vec4((rotationMatrix4 * vec4(inPosition,1.0)).xyz, 1.0);
    vec4 scaledVertex = (scaleMatrix*_scaleMatrix) * rotatedVertex;
//    vec4 positionedVertex = scaledVertex + instPosition;

    vec4 positionedVertex = vec4(inPosition+instPosition, 1.0) * (scaleMatrix*_scaleMatrix) * (rotationMatrix4*_rotMatrix) * transMatrix;
    vec4 rotatedNormal = vec4(inNormal * outlineWidth, 1.0) * (rotationMatrix4 * _rotMatrix);
//    vec4 positionedNormal = scaledNormal + instPosition;
    vec4 final = positionedVertex + rotatedNormal;

	gl_Position = vec4(final.xyz,1.0) * viewMatrix * projectionMatrix;
}
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

mat4 GetTranslationMatrixVec3(vec3 translation)
{
    // Construct a translation-only matrix
    mat4 translationMatrix = mat4(
        vec4(1.0, 0.0, 0.0, translation.x),
        vec4(0.0, 1.0, 0.0, translation.y),
        vec4(0.0, 0.0, 1.0, translation.z),
        vec4(0.0, 0.0, 0.0, 1.0) // Set the translation component
    );

    return translationMatrix;
}


void main() {
    float outlineWidth = 1.0;

	mat4 instRotMatrix = constructRotationMatrix(instRotation);
    mat4 rotMatrix = _rotMatrix * instRotMatrix;

    mat4 instScaleMatrix = mat4(
        vec4(instScale.x, 0.0, 0.0, 0.0),
        vec4(0.0, instScale.y, 0.0, 0.0),
        vec4(0.0, 0.0, instScale.z, 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );
    mat4 scaleMatrix = _scaleMatrix * instScaleMatrix;

    mat4 _transMatrix = GetTranslationMatrix(modelMatrix);
    mat4 instTransMatrix = GetTranslationMatrixVec3(instPosition);
    mat4 transMatrix = _transMatrix * instTransMatrix;


//    vec4 pos = vec4(inPosition,1.0) * scaleMatrix * rotMatrix * transMatrix;
    vec4 pos = vec4(inPosition,1.0);
//    pos = pos * scaleMatrix;
    pos = pos * instRotMatrix;
    pos = pos * transMatrix;
    pos = pos * _rotMatrix;


//    vec4 norm = vec4(inNormal,1.0) * outlineWidth * rotMatrix;
//    norm.w = 0;
    vec4 norm = vec4(inNormal,1.0) * outlineWidth;
    norm.w = 0.0;

//    norm = norm * instRotation;
//    norm = norm * transMatrix;
//    norm = norm * _rotMatrix;
//    norm = norm * inverse(transMatrix);
//    norm.w = 0;

    vec4 final = pos + norm;
    final.w = 1.0;

    gl_Position = final * viewMatrix * projectionMatrix;
}
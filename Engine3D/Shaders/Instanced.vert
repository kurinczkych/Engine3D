#version 330 core

layout (location = 0) in vec4 inPosition;
layout (location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inUV;
layout(location = 3) in vec4 inColor;
layout(location = 4) in vec3 inTangent;
layout(location = 5) in vec4 instPosition;
layout(location = 6) in vec4 instRotation;
layout(location = 7) in vec3 instScale;
layout(location = 8) in vec4 instColor;

out vec3 fragPos;
out vec3 fragNormal;
out vec2 fragTexCoord;
out vec4 fragColor;
out mat3 TBN; 
out vec3 TangentViewDir;

uniform vec3 cameraPosition;
uniform vec2 windowSize;
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform int useNormal;
uniform int useHeight;

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

mat3 inverseTranspose(mat3 m) {
    return transpose(inverse(m));
}

void main()
{
	mat3 rotationMatrix = convertQuaternionToMat3(instRotation);
	mat4 scaleMatrix = mat4(
        vec4(instScale.x, 0.0, 0.0, 0.0),
        vec4(0.0, instScale.y, 0.0, 0.0),
        vec4(0.0, 0.0, instScale.z, 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );
    vec4 rotatedVertex = vec4(rotationMatrix * inPosition.xyz, 1.0);
    vec4 scaledVertex = scaleMatrix * rotatedVertex;
    vec4 positionedVertex = scaledVertex + instPosition;


	gl_Position = positionedVertex * modelMatrix * viewMatrix * projectionMatrix;
	vec4 fragPos4 = positionedVertex * modelMatrix;

    mat3 normalMatrix = inverseTranspose(rotationMatrix);
    fragNormal = normalize(normalMatrix * inNormal.xyz);
	fragPos = vec3(fragPos4.x,fragPos4.y, fragPos4.z);
	fragTexCoord = inUV;
	fragColor = instColor;

	//normal
	vec3 T = vec3(0,0,0);
	vec3 B = vec3(0,0,0);
	vec3 N = vec3(0,0,0);
	if(useNormal == 1)
	{
		N = normalize(mat3(modelMatrix) * inNormal);
		T = normalize(mat3(modelMatrix) * inTangent);
		T = normalize(T - dot(T, N) * N);
		B = cross(N, T); 
		TBN = mat3(T, B, N);
	}

	//height
	if(useNormal == 1 && useHeight == 1)
	{
		vec3 viewDir = cameraPosition - fragPos;
		TangentViewDir = normalize(vec3(dot(viewDir, T), dot(viewDir, B), dot(viewDir, N)));
	}
}
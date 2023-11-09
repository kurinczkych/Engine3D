#version 330 core

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inUV;
layout(location = 3) in vec4 inColor;
layout(location = 4) in vec3 inTangent;
layout(location = 5) in vec3 instPosition;
layout(location = 6) in vec4 instRotation;
layout(location = 7) in vec3 instScale;
layout(location = 8) in vec4 instColor;

out vec3 gsFragPos;
out vec3 gsFragNormal;
out vec2 gsFragTexCoord;
out vec4 gsFragColor;
out mat3 gsTBN; 
out vec3 gsTangentViewDir;

uniform vec3 cameraPosition;
uniform vec2 windowSize;
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform int useNormal;
uniform int useHeight;
uniform int useBillboarding;

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

mat4 createTranslationMatrix(vec3 translation) {
    return mat4(
        vec4(1.0, 0.0, 0.0, 0.0),
        vec4(0.0, 1.0, 0.0, 0.0),
        vec4(0.0, 0.0, 1.0, 0.0),
        vec4(translation, 1.0)
    );
}

void main()
{
	mat4 scaleMatrix = mat4(
        vec4(instScale.x, 0.0, 0.0, 0.0),
        vec4(0.0, instScale.y, 0.0, 0.0),
        vec4(0.0, 0.0, instScale.z, 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );
	mat3 rotationMatrix = convertQuaternionToMat3(instRotation);
	mat4 pureRotationMatrix = mat4(
        vec4(rotationMatrix[0], 0.0),
        vec4(rotationMatrix[1], 0.0),
        vec4(rotationMatrix[2], 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );
    mat4 transMatrix = createTranslationMatrix(instPosition);

	vec4 scaleVertex = vec4(inPosition, 1.0) * scaleMatrix;
	vec4 rotatedVertex = pureRotationMatrix * scaleVertex;
    vec4 positionedVertex = transMatrix * rotatedVertex;

	gl_Position = positionedVertex * modelMatrix * viewMatrix * projectionMatrix;
	vec4 fragPos4 = positionedVertex * modelMatrix;

    if(useBillboarding == 1)
	{
		vec3 look = normalize(cameraPosition - vec3(modelMatrix * vec4(inPosition, 1.0)));
		vec3 right = normalize(cross(vec3(0.0, 1.0, 0.0), look));
		vec3 up = cross(look, right);

		// Build the billboard matrix
		mat4 billboardMat = mat4(
			vec4(right, 0.0),
			vec4(up, 0.0),
			vec4(-look, 0.0),
			vec4(0.0, 0.0, 0.0, 1.0)
		);

		gl_Position = positionedVertex * billboardMat * modelMatrix * viewMatrix * projectionMatrix;
		vec4 fragPos4 = positionedVertex * billboardMat * modelMatrix;
	}

    mat3 normalMatrix = inverseTranspose(rotationMatrix);
    gsFragNormal = normalize(normalMatrix * inNormal.xyz);
	gsFragPos = vec3(fragPos4.x,fragPos4.y, fragPos4.z);
	gsFragTexCoord = inUV;
	gsFragColor = instColor;

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
		gsTBN = mat3(T, B, N);
	}

	//height
	if(useNormal == 1 && useHeight == 1)
	{
		vec3 viewDir = cameraPosition - gsFragPos;
		gsTangentViewDir = normalize(vec3(dot(viewDir, T), dot(viewDir, B), dot(viewDir, N)));
	}
}
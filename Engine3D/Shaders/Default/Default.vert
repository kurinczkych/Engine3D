#version 330 core

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec3 inNormal;
layout (location = 2) in vec2 inUV;
layout (location = 3) in vec4 inColor;
layout (location = 4) in vec3 inTangent;
layout (location = 5) in ivec4 boneIDs;
layout (location = 6) in vec4 weights;

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

const int MAX_BONES = 100;
uniform mat4 boneMatrices[MAX_BONES];
uniform int boneCount;
uniform int useAnimation;

void main()
{
	//boneIDs contains more than 4 bones, each mesh can contain x count of bones.
	mat4 boneMatrix = 
        boneMatrices[boneIDs[0]] * weights[0] +
        boneMatrices[boneIDs[1]] * weights[1] +
        boneMatrices[boneIDs[2]] * weights[2] +
        boneMatrices[boneIDs[3]] * weights[3];

	vec4 position = vec4(inPosition,1.0);

	gl_Position = position * boneMatrix * modelMatrix * viewMatrix * projectionMatrix;
	vec4 fragPos4 = position * modelMatrix;

	if(useBillboarding == 1)
	{
		vec3 look = normalize(cameraPosition - vec3(modelMatrix * position));
		vec3 right = normalize(cross(vec3(0.0, 1.0, 0.0), look));
		vec3 up = cross(look, right);

		// Build the billboard matrix
		mat4 billboardMat = mat4(
			vec4(right, 0.0),
			vec4(up, 0.0),
			vec4(-look, 0.0),
			vec4(0.0, 0.0, 0.0, 1.0)
		);

		gl_Position = position * billboardMat * modelMatrix * viewMatrix * projectionMatrix;
		fragPos4 = position * billboardMat * modelMatrix;
	}

	mat3 rotationScaleMatrix = mat3(modelMatrix);
	vec3 col1 = normalize(rotationScaleMatrix[0]);
	vec3 col2 = normalize(rotationScaleMatrix[1]);
	vec3 col3 = normalize(rotationScaleMatrix[2]);
	mat3 rotationMatrix = mat3(col1, col2, col3);

	gsFragPos = vec3(fragPos4.x,fragPos4.y, fragPos4.z);
	gsFragTexCoord = inUV;
	gsFragNormal = inNormal * rotationMatrix;
	gsFragColor = inColor;

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
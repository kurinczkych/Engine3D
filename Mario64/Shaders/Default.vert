#version 330 core

layout (location = 0) in vec4 inPosition;
layout (location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inUV;
layout(location = 4) in vec3 inCamera;

out vec4 fragColor;
out vec2 fragTexCoord;
//out float rhw;

uniform vec2 windowSize;
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

void main()
{
//	vec3 pos = vec3(inPosition.x, inPosition.y, inPosition.z);
//	vec4 transPosition =  uModelMatrix * vec4(pos, 1.0);
//
//	vec4 viewPosition = uViewMatrix * transPosition;
//	vec4 projPosition = uProjectionMatrix * viewPosition;
//	float w = projPosition.w;
//	projPosition = projPosition / w;
//	projPosition.w = 1.0;

	gl_Position = inPosition * modelMatrix * viewMatrix * projectionMatrix;
//	gl_Position = vec4(pos, 1.0f);


//	vec3 cameraRay = pos - inCamera;
	vec3 lightDir = vec3(0.5, 1.0, 0.0f);
	lightDir = normalize(lightDir);
	float dp = max(0.1, dot(inNormal, lightDir));

	fragColor = vec4(dp,dp,dp,1.0);// * vec4(1.0, 0.0, 0.0, 0.3);
//	fragColor.a = 0.3;
	fragTexCoord = inUV;
}
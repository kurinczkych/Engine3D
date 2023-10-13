#version 330 core

layout (location = 0) in vec4 inPosition;
layout (location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inUV;

//out vec4 fragColor;
out vec3 fragPos;
out vec3 normal;
out vec2 fragTexCoord;

uniform vec2 windowSize;
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

void main()
{
	gl_Position = inPosition * modelMatrix * viewMatrix * projectionMatrix;
	vec4 fragPos4 = inPosition * modelMatrix;
	fragPos = vec3(fragPos4.x,fragPos4.y, fragPos4.z);

//	vec3 lightDir = vec3(-0.5, 1.0, 0.0f);
//	lightDir = normalize(lightDir);
//	float dp = max(0.1, dot(inNormal, lightDir));
	float dp = 0.0;

//	fragColor = vec4(dp,dp,dp,1.0);// * vec4(1.0, 0.0, 0.0, 0.3);
//	fragColor.a = 0.3;
	fragTexCoord = inUV;
	normal = inNormal;
}
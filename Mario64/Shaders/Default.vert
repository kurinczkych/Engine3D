#version 330 core

layout (location = 0) in vec4 inPosition;
layout (location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inUV;

out vec4 fragColor;
out vec2 fragTexCoord;
//out float rhw;

uniform vec2 windowSize;
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform mat4 transformMatrix;

void main()
{
	vec4 pos = inPosition * transformMatrix;
	gl_Position = inPosition * modelMatrix * viewMatrix * projectionMatrix;


	vec3 lightDir = vec3(0.5, 1.0, 0.0f);
	lightDir = normalize(lightDir);
	float dp = max(0.1, dot(inNormal, lightDir));

	fragColor = vec4(dp,dp,dp,1.0);// * vec4(1.0, 0.0, 0.0, 0.3);
//	fragColor.a = 0.3;
	fragTexCoord = inUV;
}
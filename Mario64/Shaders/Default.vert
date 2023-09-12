#version 330 core

layout (location = 0) in vec4 inPosition;
layout(location = 1) in vec4 inColor;
layout(location = 2) in vec3 inUV;

out vec4 fragColor;
out vec2 fragTexCoord;

void main()
{
	gl_Position = vec4(inPosition.x, inPosition.y, inPosition.z, 1.0);
//	gl_Position = vec4(inPosition, 1.0);
	fragColor = inColor;

	fragTexCoord = vec2(inUV.x, inUV.y);

//	fragW = inUV.z;
//	fragTexCoord = inUV / inPosition.w;
//	fragTexCoord = vec2(inUV.x/inUV.z, inUV.y/inUV.z);
}
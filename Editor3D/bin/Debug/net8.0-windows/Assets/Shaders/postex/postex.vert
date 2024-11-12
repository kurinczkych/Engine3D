#version 330 core

layout (location = 0) in vec4 inPosition;
layout (location = 1) in vec4 inColor;
layout(location = 2) in vec2 inUV;

//out vec4 fragColor;
out vec4 fragColor;
out vec2 fragTexCoord;

uniform vec2 windowSize;

void main()
{
	//Converting to NDC coordinates
//    float x = (2.0 * inPosition.x / windowSize.x) - 1.0;
//    float y = (2.0 * inPosition.y / windowSize.y) - 1.0;
	gl_Position = vec4(inPosition.x, inPosition.y, -1.0, 1.0);
//	gl_Position = vec4(x,y,-1.0,1.0);

	fragColor = inColor;
	fragTexCoord = inUV;
}
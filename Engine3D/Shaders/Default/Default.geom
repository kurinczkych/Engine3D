#version 330 core

layout(triangles) in;
layout(triangle_strip, max_vertices = 3) out;

in vec3 fragPos[];
in vec3 fragNormal[];
in vec2 fragTexCoord[];
in vec4 fragColor[];
in mat3 TBN[];
in vec3 TangentViewDir[];

out vec3 gsFragPos;
out vec3 gsFragNormal;
out vec2 gsFragTexCoord;
out vec4 gsFragColor;
out mat3 gsTBN;
out vec3 gsTangentViewDir;

void main()
{
	for(int i = 0; i < gl_in.length(); i++) {
        // Pass-through the vertex attributes from the Vertex Shader
        gsFragPos = fragPos[i];
        gsFragNormal = fragNormal[i];
        gsFragTexCoord = fragTexCoord[i];
        gsFragColor = fragColor[i];
        gsTBN = TBN[i];
        gsTangentViewDir = TangentViewDir[i];
        
        // Pass-through the vertex position
        gl_Position = gl_in[i].gl_Position;

        EmitVertex();
    }
    EndPrimitive();
}
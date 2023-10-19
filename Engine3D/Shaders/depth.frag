#version 330 core

in vec2 TexCoord;
out vec4 FragColor;

void main()
{
    float near = 0.1;  // Adjust according to your near plane
    float far = 1000.0;  // Adjust according to your far plane
    float depth = gl_FragCoord.z / gl_FragCoord.w;  // This is the linearized depth
    float linearDepth = (2.0 * near) / (far + near - depth * (far - near));
    FragColor = vec4(vec3(linearDepth), 1.0);

//    // Fetch the depth value directly
//    float depthValue = gl_FragCoord.z;
//    
//    // Visualize the depth
//    FragColor = vec4(vec3(depthValue), 1.0);
}

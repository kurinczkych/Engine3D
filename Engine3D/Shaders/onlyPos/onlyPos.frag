#version 330 core

in vec4 fragColor;
out vec4 FragColor;

void main()
{
    FragColor = fragColor * vec4(1.0,1.0,1.0,0.5);
//    FragColor = vec4(1.0,1.0,1.0,1.0);
}


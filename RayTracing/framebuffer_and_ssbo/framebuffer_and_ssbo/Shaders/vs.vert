#version 330

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec2 vtexCoords;

out vec2 texCoords;

void main()
{
	texCoords = vtexCoords;
	gl_Position = vec4(vPosition, 1.0);
}
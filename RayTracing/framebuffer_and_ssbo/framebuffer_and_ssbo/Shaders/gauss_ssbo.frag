#version 430

layout (std430, binding=0) buffer Kernel_Buffer
{ 
  float kernel_data[];
} kernel_buffer;

uniform sampler2D tex;
uniform vec2 imageSize;
uniform int size;

in vec2 texCoords;
out vec4 fragColor;

void main()
{
	int radius = size / 2;
	fragColor = vec4 (0.0);
	float norm = 0.0;
	for(int i = 0; i < size; ++i)
	{
		for(int j = 0; j < size; ++j)
		{
			vec2 gaussianTexCoord = vec2(i+0.5, j+0.5) / size;
			vec2 displacement = vec2(i - radius, j - radius) / imageSize;
			float gaussian = kernel_buffer.kernel_data[i * size + j];
			norm += gaussian;
			fragColor += texture(tex, texCoords + displacement) * gaussian;
		}
	}

	fragColor /= norm;
}
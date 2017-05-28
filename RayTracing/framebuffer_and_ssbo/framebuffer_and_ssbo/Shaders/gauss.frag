#version 330

uniform sampler2D tex;
uniform sampler2D kernel;
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
			vec4 gaussian = texture(kernel, gaussianTexCoord);
			norm += gaussian.r;
			fragColor += texture(tex, texCoords + displacement) * gaussian;
		}
	}

	fragColor /= norm;
}
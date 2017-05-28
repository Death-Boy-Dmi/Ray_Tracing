using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.IO;

namespace framebuffer_and_ssbo
{
    public partial class Form1 : Form
    {
        //bitmap
        private Bitmap m_bitmap;
        //texture to display
        private int m_texture_to_display;
        //buffers
        private int m_vertex_buffer;
        private int m_texcords_buffer;
        private int m_fb_texcords_buffer;
        //vertex array
        private int m_vertex_arrays;
        private int m_fb_vertex_arrays;
        //shaders
        private int m_program_id;
        private int m_program_gauss_id;
        private int m_program_gauss_ssbo_id;
        private int m_vs_id;
        private int m_ps_id;
        //framebuffer
        private int m_frambuffer_texture;
        private int m_frambuffer;
        //gaussian
        private float [,] m_gaussian_kernel = new float [1,1];
        private float m_sigma = 1.0f;
        private int m_radius = 1;
        private int m_packed_gaussian;
        private int m_ssbo_gaussian;

        //our quad
        private float [] quad = { -1, -1, -1, 1, 1, 1, 1, -1 };
        private float [] tex_coord = { 0, 1, 0, 0, 1, 0, 1, 1 };
        private float [] fb_tex_coord = { 0, 0, 0, 1, 1, 1, 1, 0 };

        public Form1()
        {
            InitializeComponent();

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files | *.png; *.jpg; *.bmp | All files (*.*) | *.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                m_bitmap = new Bitmap(dialog.FileName);
            }
            else
                this.Close();
        }

        void create_kernel(float sigma, int radius)
        {
            int size = radius * 2 + 1;
            m_gaussian_kernel = new float[size, size];
            
            for (int i = 0; i < size; ++i)
                for (int j = 0; j < size; ++j)
                {
                    m_gaussian_kernel[i, j] = (float)(Math.Exp(-((i - radius) * (i - radius) + (j - radius) * (j - radius)) /(2.0f * sigma * sigma)) /
                        Math.Sqrt(2.0f * Math.PI) / sigma);
                }
        }

        void pack_gaussian_texture()
        {
            create_kernel(m_sigma, m_radius);

            //copy to texture
            GL.BindTexture(TextureTarget.Texture2D, m_packed_gaussian);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Luminance, m_gaussian_kernel.GetLength(0), m_gaussian_kernel.GetLength(1),
                0, PixelFormat.Luminance, PixelType.Float, m_gaussian_kernel);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            //copy to ssbo
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, m_ssbo_gaussian);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, (IntPtr)(sizeof(float)*m_gaussian_kernel.Length), m_gaussian_kernel, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        void loadShader(String filename, ShaderType type, int program, out int address)
        {
            address = GL.CreateShader(type);
            using (System.IO.StreamReader sr = new StreamReader(filename))
            {
                GL.ShaderSource(address, sr.ReadToEnd());
            }
            GL.CompileShader(address);
            GL.AttachShader(program, address);
            Console.WriteLine(GL.GetShaderInfoLog(address));
        }

        private void InitializeOpenGL()
        {
            glControl1.MakeCurrent();
            GL.ClearColor(0.0f, 1.0f, 0.0f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);

            //texture
            m_texture_to_display = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, m_texture_to_display);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);

            System.Drawing.Imaging.BitmapData bitmap_data = m_bitmap.LockBits(new Rectangle(0, 0, m_bitmap.Width, m_bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, m_bitmap.Width, m_bitmap.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bitmap_data.Scan0);

            m_bitmap.UnlockBits(bitmap_data);

            ErrorCode code = GL.GetError();

            m_packed_gaussian = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, m_packed_gaussian);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Luminance, m_gaussian_kernel.GetLength(0), m_gaussian_kernel.GetLength(1), 0, PixelFormat.Luminance, PixelType.Float, (IntPtr)null);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            //buffers

            m_vertex_buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_vertex_buffer);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * quad.Length), quad, BufferUsageHint.StaticDraw);

            m_texcords_buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_texcords_buffer);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * tex_coord.Length), tex_coord, BufferUsageHint.StaticDraw);


            m_fb_texcords_buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_fb_texcords_buffer);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * fb_tex_coord.Length), fb_tex_coord, BufferUsageHint.StaticDraw);

            m_vertex_arrays = GL.GenVertexArray();
            GL.BindVertexArray(m_vertex_arrays);
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_vertex_buffer);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_texcords_buffer);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            m_fb_vertex_arrays = GL.GenVertexArray();
            GL.BindVertexArray(m_fb_vertex_arrays);
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_vertex_buffer);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_fb_texcords_buffer);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);


            GL.BindVertexArray(0);
            ErrorCode err = GL.GetError();


            m_ssbo_gaussian = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, m_ssbo_gaussian);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, (IntPtr)1, (IntPtr)null, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, m_ssbo_gaussian);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            //framebuffers

            m_frambuffer = GL.GenFramebuffer();
            m_frambuffer_texture = GL.GenTexture();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_frambuffer);
            GL.BindTexture(TextureTarget.Texture2D, m_frambuffer_texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, m_bitmap.Width, m_bitmap.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)null);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, m_frambuffer_texture, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void InitShaders()
        {
            m_program_id = GL.CreateProgram();
            loadShader("..\\..\\Shaders\\vs.vert", ShaderType.VertexShader, m_program_id, out m_vs_id);
            loadShader("..\\..\\Shaders\\ps.frag", ShaderType.FragmentShader, m_program_id, out m_ps_id);
            GL.LinkProgram(m_program_id);


            m_program_gauss_id = GL.CreateProgram();
            loadShader("..\\..\\Shaders\\vs.vert", ShaderType.VertexShader, m_program_gauss_id, out m_vs_id);
            loadShader("..\\..\\Shaders\\gauss.frag", ShaderType.FragmentShader, m_program_gauss_id, out m_ps_id);
            GL.LinkProgram(m_program_gauss_id);

            m_program_gauss_ssbo_id = GL.CreateProgram();
            loadShader("..\\..\\Shaders\\vs.vert", ShaderType.VertexShader, m_program_gauss_ssbo_id, out m_vs_id);
            loadShader("..\\..\\Shaders\\gauss_ssbo.frag", ShaderType.FragmentShader, m_program_gauss_ssbo_id, out m_ps_id);
            GL.LinkProgram(m_program_gauss_ssbo_id);

            int status = 0;
            GL.GetProgram(m_program_id, GetProgramParameterName.LinkStatus, out status);
            Console.WriteLine(GL.GetProgramInfoLog(m_program_id));
            GL.GetProgram(m_program_gauss_id, GetProgramParameterName.LinkStatus, out status);
            Console.WriteLine(GL.GetProgramInfoLog(m_program_gauss_id));
            GL.GetProgram(m_program_gauss_ssbo_id, GetProgramParameterName.LinkStatus, out status);
            Console.WriteLine(GL.GetProgramInfoLog(m_program_gauss_ssbo_id));
        }

        private void PrepareViewPort()
        {
            GL.Viewport(0, 0, glControl1.Width, glControl1.Height);
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            glControl1.MakeCurrent();

            PrepareViewPort();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int tex_loc = GL.GetUniformLocation(m_program_id, "tex");

            GL.UseProgram(m_program_id);
            GL.Uniform1(tex_loc, 0);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, m_texture_to_display);

            GL.BindVertexArray(m_vertex_arrays);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);

            ErrorCode gl_err = GL.GetError();

            glControl1.SwapBuffers();
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            InitShaders();
            InitializeOpenGL();
        }

        private void gaussToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            pack_gaussian_texture();

            glControl1.MakeCurrent();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_frambuffer);

            GL.Viewport(0, 0, m_bitmap.Width, m_bitmap.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int tex_loc = GL.GetUniformLocation(m_program_gauss_id, "tex");
            int ker_loc = GL.GetUniformLocation(m_program_gauss_id, "kernel");
            int size_loc = GL.GetUniformLocation(m_program_gauss_id, "size");
            int imsize_loc = GL.GetUniformLocation(m_program_gauss_id, "imageSize");

            GL.UseProgram(m_program_gauss_id);
            GL.Uniform1(tex_loc, 0);
            GL.Uniform1(ker_loc, 1);
            GL.Uniform1(size_loc, m_gaussian_kernel.GetLength(0));
            GL.Uniform2(imsize_loc, (float)m_bitmap.Width, (float)m_bitmap.Height);


            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, m_texture_to_display);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, m_packed_gaussian);

            GL.BindVertexArray(m_fb_vertex_arrays);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.UseProgram(0);

            GL.BindTexture(TextureTarget.Texture2D, m_texture_to_display);
            GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, m_bitmap.Width, m_bitmap.Height);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            glControl1.Invalidate();
            GL.Finish();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var elapsedTicks = watch.ElapsedTicks;

            this.toolStripStatusLabel1.Text = "Time taken: " + elapsedTicks + "ticks, " + elapsedMs + "ms";
        }

        private void textBoxRadius_TextChanged(object sender, EventArgs e)
        {
            this.m_radius = int.Parse(this.textBoxRadius.Text);
        }

        private void textBoxSigma_TextChanged(object sender, EventArgs e)
        {
            this.m_sigma = float.Parse(this.textBoxSigma.Text);
        }

        private void gaussssboToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            pack_gaussian_texture();

            glControl1.MakeCurrent();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_frambuffer);

            GL.Viewport(0, 0, m_bitmap.Width, m_bitmap.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int tex_loc = GL.GetUniformLocation(m_program_gauss_ssbo_id, "tex");
            int size_loc = GL.GetUniformLocation(m_program_gauss_ssbo_id, "size");
            int imsize_loc = GL.GetUniformLocation(m_program_gauss_ssbo_id, "imageSize");

            GL.UseProgram(m_program_gauss_ssbo_id);
            GL.Uniform1(tex_loc, 0);
            GL.Uniform1(size_loc, m_gaussian_kernel.GetLength(0));
            GL.Uniform2(imsize_loc, (float)m_bitmap.Width, (float)m_bitmap.Height);


            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, m_texture_to_display);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, m_ssbo_gaussian);

            GL.BindVertexArray(m_fb_vertex_arrays);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.UseProgram(0);

            GL.BindTexture(TextureTarget.Texture2D, m_texture_to_display);
            GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, m_bitmap.Width, m_bitmap.Height);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            glControl1.Invalidate();
            GL.Finish();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var elapsedTicks = watch.ElapsedTicks;

            this.toolStripStatusLabel1.Text = "Time taken: " + elapsedTicks + "ticks, " + elapsedMs + "ms";
        }
    }
}

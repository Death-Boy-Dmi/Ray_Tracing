using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
namespace RayTracing
{
    class View
    {
        int BasicProgramID;
        int BasicVertexSheder;
        int BasicFragmentShader;
        int vbo_position;
        int attribute_vpos;
        int uniform_pos;
        Vector3 campos;
        int uniform_aspect;
        double aspect;
        Vector3[] vertdata;
        public void Setup(int w, int h)
        {
            string str = GL.GetString(StringName.ShadingLanguageVersion);
            GL.ClearColor(Color.DarkGray);
            InitShaders();
            InitBuffer();
        }
        public void Update()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);
            GL.EnableVertexAttribArray(attribute_vpos);//on
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);//отрисовываем буф объект
            GL.DisableVertexAttribArray(attribute_vpos);//off

        }
        void loadShader(String filename, ShaderType type, int program, out int address)
        {
            address = GL.CreateShader(type);
            using (System.IO.StreamReader sr = new StreamReader(filename))
            {
                GL.ShaderSource(address, sr.ReadToEnd());
            } GL.CompileShader(address);
            GL.AttachShader(program, address);
            Console.WriteLine(GL.GetShaderInfoLog(address));
        }
        private void InitShaders()
        {
            BasicProgramID = GL.CreateProgram();//создали объект шейдерной программы
            loadShader("D:\\Библиотека\\Документы\\GitHub\\Ray_Tracing\\RayTracing\\RayTracing\\raytracing.vert", ShaderType.VertexShader,
                BasicProgramID, out BasicVertexSheder);
            loadShader("D:\\Библиотека\\Документы\\GitHub\\Ray_Tracing\\RayTracing\\RayTracing\\raytracing.frag", ShaderType.FragmentShader,
                BasicProgramID, out BasicFragmentShader);
            GL.LinkProgram(BasicProgramID);
            //линковка успешна?
            int status = 0;
            GL.GetProgram(BasicProgramID, GetProgramParameterName.LinkStatus, out status);
            Console.WriteLine(GL.GetProgramInfoLog(BasicProgramID));
        }
        private void InitBuffer()
        {
            vertdata = new Vector3[] 
            {
                new Vector3(-1f, -1f, 0f),
                new Vector3(1f, -1f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(-1f, 1f, 0f)
            };
            GL.GenBuffers(1, out vbo_position);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_position);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(vertdata.Length * Vector3.SizeInBytes), vertdata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(attribute_vpos, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.Uniform3(uniform_pos, campos);
            GL.Uniform1(uniform_aspect, aspect);
            GL.UseProgram(BasicProgramID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
    }
}

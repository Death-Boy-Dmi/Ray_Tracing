using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace RayTracing
{
    public partial class Form1 : Form
    {
        View view;
        bool loaded = false;
        public Form1()
        {
            InitializeComponent();
            view = new View();
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            loaded = true;
            view.Setup(glControl1.Width, glControl1.Height);
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded) return;
            view.Update();
            glControl1.SwapBuffers();
            GL.UseProgram(0);
        }
    }
}

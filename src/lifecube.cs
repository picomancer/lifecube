
using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace Picomancer.LifeCube
{
    public class App : IDisposable
    {
        public GameWindow win;
        public uint face_width, face_height;
        public MyMesh mesh;
        public Matrix4 m_model;
        public float[] vertex;
        public uint[] vbo_id;

        public class VB
        {
            // don't use an enum for this to avoid inability to cast to int
            public const int cube_vertex = 0;
            public const int cube_index = 1;
            public const int count = 2;
        };

        public const uint VERTEX_SIZE = 7;
        // color  : 4ub
        // vertex : 3f
        // normal : 3f

        public static float[] face2color = {
            intBitsToFloat(0xFF0000FF),
            intBitsToFloat(0xFFFFFFFF),
            intBitsToFloat(0xFF00FFFF),
            intBitsToFloat(0xFFFF0000),
            intBitsToFloat(0xFFFF00FF),
            intBitsToFloat(0xFFFFFF00)
            };

        public static float BLACK = intBitsToFloat(0xFF000000);

        // 6 faces:
        //
        // xy, z=0    xz, y=0    yz, x=0
        // xy, z=1    xz, y=1    yz, z=1
        public static float[] face_x0 =
            {-0.5f, -0.5f, -0.5f, -0.5f, -0.5f,  0.5f};
        public static float[] face_y0 =
            {-0.5f, -0.5f, -0.5f, -0.5f,  0.5f, -0.5f};
        public static float[] face_z0 =
            {-0.5f, -0.5f, -0.5f,  0.5f, -0.5f, -0.5f};

        public static float[] face_ux =
            {1.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f};
        public static float[] face_uy =
            {0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f};
        public static float[] face_uz =
            {0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f};

        public static float[] face_vx =
            {0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f};
        public static float[] face_vy =
            {1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f};
        public static float[] face_vz =
            {0.0f, 1.0f, 1.0f, 0.0f, 1.0f, 1.0f};

        public static float[] face_nx =
            {0.0f, 0.0f,  1.0f, 0.0f, 0.0f,  1.0f};
        public static float[] face_ny =
            {0.0f, -1.0f, 0.0f, 0.0f, -1.0f, 0.0f};
        public static float[] face_nz =
            { 1.0f, 0.0f, 0.0f,  1.0f, 0.0f, 0.0f};

        public static float intBitsToFloat(uint x)
        {
            // based on http://stackoverflow.com/questions/18079000/intbitstofloat-method-in-java-vs-c
            byte []b = BitConverter.GetBytes(x);
            float result = BitConverter.ToSingle(b, 0);
            return result;
        }

        public App()
        {
            Console.WriteLine("App()");
            this.win = new GameWindow();

            this.face_width = 20;
            this.face_height = 20;

            this.win.Load += (sender, e) =>
            {
                // setup settings, load textures, sounds
                this.win.VSync = VSyncMode.On;

                this.vbo_id = new uint[VB.count];
                GL.GenBuffers(VB.count, vbo_id);

                vertex = new float [6*(this.face_width)*(this.face_height)*4*VERTEX_SIZE];
                ushort[] index = new ushort[6*(this.face_width)*(this.face_height)*6*VERTEX_SIZE];

                uint w = this.face_width;
                uint k = 0, nv = 0, ni = 0;
                for(uint face=0;face<6;face++)
                {
                    // TODO:  adjust multiplier for non-cubic case
                    float x0 = face_x0[face] * w;
                    float y0 = face_y0[face] * w;
                    float z0 = face_z0[face] * w;

                    float ux = face_ux[face];
                    float uy = face_uy[face];
                    float uz = face_uz[face];

                    float vx = face_vx[face];
                    float vy = face_vy[face];
                    float vz = face_vz[face];

                    float nx = face_nx[face];
                    float ny = face_ny[face];
                    float nz = face_nz[face];
                    
                    for(uint v=0;v<this.face_height;v++)
                    {
                        for(uint u=0;u<this.face_width;u++)
                        {
                            float c = (((u^v)&1) != 0) ? face2color[face] : BLACK;

                            vertex[k   ] = c;
                            vertex[k+1 ] = x0 + ux * (u  ) + vx * (v  );
                            vertex[k+2 ] = y0 + uy * (u  ) + vy * (v  );
                            vertex[k+3 ] = z0 + uz * (u  ) + vz * (v  );
                            vertex[k+4 ] = nx;
                            vertex[k+5 ] = ny;
                            vertex[k+6 ] = nz;

                            vertex[k+7 ] = c;
                            vertex[k+8 ] = x0 + ux * (u+1) + vx * (v  );
                            vertex[k+9 ] = y0 + uy * (u+1) + vy * (v  );
                            vertex[k+10] = z0 + uz * (u+1) + vz * (v  );
                            vertex[k+11] = nx;
                            vertex[k+12] = ny;
                            vertex[k+13] = nz;

                            vertex[k+14] = c;
                            vertex[k+15] = x0 + ux * (u  ) + vx * (v+1);
                            vertex[k+16] = y0 + uy * (u  ) + vy * (v+1);
                            vertex[k+17] = z0 + uz * (u  ) + vz * (v+1);
                            vertex[k+18] = nx;
                            vertex[k+19] = ny;
                            vertex[k+20] = nz;

                            vertex[k+21] = c;
                            vertex[k+22] = x0 + ux * (u+1) + vx * (v+1);
                            vertex[k+23] = y0 + uy * (u+1) + vy * (v+1);
                            vertex[k+24] = z0 + uz * (u+1) + vz * (v+1);
                            vertex[k+25] = nx;
                            vertex[k+26] = ny;
                            vertex[k+27] = nz;

                            index [ni  ] = (ushort) (nv  );
                            index [ni+1] = (ushort) (nv+1);
                            index [ni+2] = (ushort) (nv+2);

                            index [ni+3] = (ushort) (nv+2);
                            index [ni+4] = (ushort) (nv+1);
                            index [ni+5] = (ushort) (nv+3);

                            k += 4*VERTEX_SIZE;
                            nv += 4;
                            ni += 6;
                        }
                    }
                }

                // setup vertex buffer
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[VB.cube_vertex]);
                GL.BufferData(BufferTarget.ArrayBuffer,
                              (IntPtr) (vertex.Length * sizeof(float)),
                              vertex,
                              BufferUsageHint.StreamDraw);

                // setup index buffer
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, vbo_id[VB.cube_index]);
                GL.BufferData(BufferTarget.ElementArrayBuffer,
                              (IntPtr) (index.Length * sizeof(ushort)),
                              index,
                              BufferUsageHint.StaticDraw);

                mesh.index_count = ni;

                m_model = Matrix4.CreateScale(0.025f);

                return;
            };

            this.win.Unload += (sender, e) =>
            {
                GL.DeleteBuffers((int) VB.count, vbo_id);
            };

            this.win.Resize += (sender, e) =>
            {
                GL.Viewport(0, 0, this.win.Width, this.win.Height);

                return;
            };

            this.win.UpdateFrame += (sender, e) =>
            {
                // add game logic, input handling
                if (this.win.Keyboard[Key.Escape])
                {
                    this.win.Exit();
                }
                if (this.win.Keyboard[Key.Left])
                {
                    m_model = m_model*Matrix4.CreateRotationY(-.025f);
                }
                if (this.win.Keyboard[Key.Right])
                {
                    m_model = m_model*Matrix4.CreateRotationY(.025f);
                }
                if (this.win.Keyboard[Key.Up])
                {
                    m_model = m_model*Matrix4.CreateRotationX(.025f);
                }
                if (this.win.Keyboard[Key.Down])
                {
                    m_model = m_model*Matrix4.CreateRotationX(-.025f);
                }

                return;
            };

            this.win.RenderFrame += (sender, e) =>
            {
                // render graphics
                GL.Enable(EnableCap.DepthTest);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();

                //GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
                GL.MultMatrix(ref m_model);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[VB.cube_vertex]);

                GL.ColorPointer(4, ColorPointerType.UnsignedByte, (int) VERTEX_SIZE * sizeof(float), (IntPtr) (0 * sizeof(float)));
                GL.EnableClientState(ArrayCap.ColorArray);

                GL.VertexPointer(3,      VertexPointerType.Float, (int) VERTEX_SIZE * sizeof(float), (IntPtr) (1 * sizeof(float)));
                GL.EnableClientState(ArrayCap.VertexArray);

                GL.NormalPointer(        NormalPointerType.Float, (int) VERTEX_SIZE * sizeof(float), (IntPtr) (4 * sizeof(float)));
                GL.EnableClientState(ArrayCap.NormalArray);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[VB.cube_index]);
                GL.DrawElements(PrimitiveType.Triangles, (int) mesh.index_count, DrawElementsType.UnsignedShort, IntPtr.Zero);

                /*
                GL.Begin(BeginMode.Triangles);

                GL.Color3(Color.MidnightBlue);
                GL.Vertex2(-1.0f, 1.0f);
                GL.Color3(Color.SpringGreen);
                GL.Vertex2(0.0f, -1.0f);
                GL.Color3(Color.Ivory);
                GL.Vertex2(1.0f, 1.0f);

                GL.End();
                */

                this.win.SwapBuffers();

                return;
            };
 
            return;
        }

        public void Dispose()
        {
            Console.WriteLine("App.Dispose()");
            this.win.Dispose();
            return;
        }

        [STAThread]
        public static void Main()
        {
            using (var app = new App())
            {
                // Run the game at 60 updates per second
                app.win.Run(60.0);
            }

            return;
        }

        public struct MyMesh
        {
            public uint index_count;
        }
    }
}

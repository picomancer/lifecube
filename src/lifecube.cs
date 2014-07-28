
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
        public float[] vertex_base;
        public float[] vertex;
        public uint[] vbo_id;
        public uint[] face_start_offset;
        public int act_x, act_y, act_face;

        // relative location of adjacent cells
        public int[][] cell_topo;

        public bool[] state, new_state;
        public bool[][] rule;

        public bool sim_paused;
        public int sim_frame_countdown;

        public int sim_frames_per_tick; // frames per tick

        public bool show_highlight;

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
            intBitsToFloat(0xFF000040),
            intBitsToFloat(0xFF404040),
            intBitsToFloat(0xFF004040),
            intBitsToFloat(0xFF400000),
            intBitsToFloat(0xFF400040),
            intBitsToFloat(0xFF404000)
            };

        public static float BLACK = intBitsToFloat(0xFF000000);
        public static float C_ACTIVE = intBitsToFloat(0xFF808000);
        public static float C_NBHD = intBitsToFloat(0xFF008000);
        public static float C_ON = intBitsToFloat(0xFF00FF00);

        // 6 faces:
        //
        // xy, z=0    xz, y=0    yz, x=0
        // xy, z=1    xz, y=1    yz, x=1
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

        public static int[] dir4_dx =
            { 1, 0, -1,  0 };
        public static int[] dir4_dy =
            { 0, 1,  0, -1 };

        public static int[] dir8_dx =
            { 1, 1, 0, -1, -1, -1,  0,  1 };
        public static int[] dir8_dy =
            { 0, 1, 1,  1,  0, -1, -1, -1 };

        // 0  1  2  3  4  5
        // z- y- x- z+ y+ x+
        // xy xz yz xy xz yz
        //
        // 0: x+ y+ x- y-    5 4 2 1
        // 1: x+ z+ x- z-    5 3 2 0
        // 2: y+ z+ y- z-    4 3 1 0
        // 3: x+ y+ x- y-    5 4 2 1
        // 4: x+ z+ x- z-    5 3 2 0
        // 5: y+ z+ y- z-    4 3 1 0
        //

        // face,wrapx,wrapy,delta_axis
        // wrapx,wrapy : Beginning coordinate of target face
        // delta_axis  : Whether the shared axis is axis 0 or 1 of the new face

        public static int[,,] adj_side = new int[,,]
            {{ {5,0,0,0}, {4,0,0,0}, {2,0,0,0}, {1,0,0,0} },
             { {5,0,0,1}, {3,0,0,0}, {2,0,0,1}, {0,0,0,0} },
             { {4,0,0,1}, {3,0,0,1}, {1,0,0,1}, {0,0,0,1} },
             { {5,0,1,0}, {4,0,1,0}, {2,0,1,0}, {1,0,1,0} },
             { {5,1,0,1}, {3,0,1,0}, {2,1,0,1}, {0,0,1,0} },
             { {4,1,0,1}, {3,1,0,1}, {1,1,0,1}, {0,1,0,1} }};

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

            this.act_x = 0;
            this.act_y = 0;
            this.act_face = 0;

            this.win.Load += (sender, e) =>
            {
                // setup settings, load textures, sounds
                this.win.VSync = VSyncMode.On;

                this.vbo_id = new uint[VB.count];
                GL.GenBuffers(VB.count, vbo_id);

                vertex = new float [6*(this.face_width)*(this.face_height)*4*VERTEX_SIZE];
                ushort[] index = new ushort[6*(this.face_width)*(this.face_height)*6*VERTEX_SIZE];

                face_start_offset = new uint[6];

                uint w = this.face_width;
                uint k = 0, nv = 0, ni = 0;
                for(uint face=0;face<6;face++)
                {
                    face_start_offset[face] = k;

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
                            vertex[k   ] = BLACK;
                            vertex[k+1 ] = x0 + ux * (u  ) + vx * (v  );
                            vertex[k+2 ] = y0 + uy * (u  ) + vy * (v  );
                            vertex[k+3 ] = z0 + uz * (u  ) + vz * (v  );
                            vertex[k+4 ] = nx;
                            vertex[k+5 ] = ny;
                            vertex[k+6 ] = nz;

                            vertex[k+7 ] = BLACK;
                            vertex[k+8 ] = x0 + ux * (u+1) + vx * (v  );
                            vertex[k+9 ] = y0 + uy * (u+1) + vy * (v  );
                            vertex[k+10] = z0 + uz * (u+1) + vz * (v  );
                            vertex[k+11] = nx;
                            vertex[k+12] = ny;
                            vertex[k+13] = nz;

                            vertex[k+14] = BLACK;
                            vertex[k+15] = x0 + ux * (u  ) + vx * (v+1);
                            vertex[k+16] = y0 + uy * (u  ) + vy * (v+1);
                            vertex[k+17] = z0 + uz * (u  ) + vz * (v+1);
                            vertex[k+18] = nx;
                            vertex[k+19] = ny;
                            vertex[k+20] = nz;

                            vertex[k+21] = BLACK;
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

                for(int face=0;face<6;face++)
                {
                    for(int y=0;y<this.face_height;y++)
                    {
                        for(int x=0;x<this.face_width;x++)
                        {
                            float c = (((x^y)&1) != 0) ? face2color[face] : BLACK;
                            this.set_color(face, x, y, c);
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

                vertex_base = new float[vertex.Length];
                Array.Copy(vertex, vertex_base, vertex.Length);

                this.init_topology();
                this.state = new bool[6*(this.face_width)*(this.face_height)];
                this.new_state = new bool[6*(this.face_width)*(this.face_height)];

                this.rule = new bool[2][];
                this.rule[0] = new bool[9];
                this.rule[1] = new bool[9];
                this.rule[0][3] = true;
                this.rule[1][2] = true;
                this.rule[1][3] = true;

                this.state[4*this.face_width+2] = true;
                this.state[5*this.face_width  ] = true;
                this.state[5*this.face_width+2] = true;
                this.state[6*this.face_width+1] = true;
                this.state[6*this.face_width+2] = true;

                this.sim_paused = true;
                this.sim_frames_per_tick = 8;

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

                if (!this.sim_paused)
                {
                    this.sim_frame_countdown--;
                    if (this.sim_frame_countdown <= 0)
                    {
                        this.update_state();
                        this.sim_frame_countdown = this.sim_frames_per_tick;
                    }
                }

                this.update_mesh();

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

            this.win.Keyboard.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.U)
                {
                    move_active(0, -1, 0);
                }
                if (e.Key == Key.H)
                {
                    move_active(-1, 0, 0);
                }
                if (e.Key == Key.M)
                {
                    move_active(0, 1, 0);
                }
                if (e.Key == Key.K)
                {
                    move_active(1, 0, 0);
                }
                if (e.Key == Key.F)
                {
                    move_active(0, 0, -1);
                }
                if (e.Key == Key.G)
                {
                    move_active(0, 0,  1);
                }
                if (e.Key == Key.T)
                {
                    sim_paused = !sim_paused;
                    update_state();
                }
                if (e.Key == Key.Y)
                {
                    show_highlight = !show_highlight;
                }
                return;
            };
 
            return;
        }

        public void init_topology()
        {
            int[][] result = new int[6*this.face_width*this.face_height][];
            uint i = 0;
            int w = (int) this.face_width, h = (int) this.face_height;

            for(int face=0;face<6;face++)
            {
                for(int y=0;y<this.face_height;y++)
                {
                    for(int x=0;x<this.face_width;x++)
                    {
                        int[][] t = get_adj(face, x, y);
                        int c = 0;
                        for(uint j=0;j<t.Length;j++)
                            c += ((t[j][0] >= 0) ? 1 : 0);
                        int[] a = new int[c];
                        for(uint j=0,k=0;j<t.Length;j++)
                        {
                            if (t[j][0] >= 0)
                                a[k++] = (t[j][0]*h + t[j][2])*w + t[j][1];
                        }
                        result[i++] = a;
                    }
                }
            }
            this.cell_topo = result;
            return;
        }

        public void update_state()
        {
            for(int i=0;i<state.Length;i++)
            {
                int []a = cell_topo[i];
                int c = 0;
                for(int j=0;j<a.Length;j++)
                    c += (state[a[j]] ? 1 : 0);
                new_state[i] = rule[state[i] ? 1 : 0][c];
            }
            bool[] t = state;
            state = new_state;
            new_state = t;

            return;
        }

        public void set_color(int face, int x, int y, float c)
        {
            uint i = face_start_offset[face];
            i += (uint) (4*VERTEX_SIZE*(y * this.face_width + x));

            vertex[i              ] = c;
            vertex[i+  VERTEX_SIZE] = c;
            vertex[i+2*VERTEX_SIZE] = c;
            vertex[i+3*VERTEX_SIZE] = c;

            return;
        }

        public int[][] get_adj(int face, int x, int y)
        {
            int[][] result = new int[8][];
            // (face, x, y) tuples

            int wm1 = ((int) this.face_width)-1;
            int hm1 = ((int) this.face_height)-1;

            for(int i=0;i<8;i++)
            {
                int fp, xp, yp;
                int bindex;

                xp = x+dir8_dx[i];
                yp = y+dir8_dy[i];

                int cc = (xp >= this.face_width ) ? 1 : 0;
                cc    |= (yp >= this.face_height) ? 2 : 0;
                cc    |= (xp < 0) ? 4 : 0;
                cc    |= (yp < 0) ? 8 : 0;

                result[i] = new int[3];

                switch(cc)
                {
                case 0:
                    result[i][0] = face;
                    result[i][1] = xp;
                    result[i][2] = yp;

                    break;
                case 1:
                    // wrap in +x direction
                    bindex = 0;

                    result[i][0] = adj_side[face,bindex,0];
                    result[i][1] = (adj_side[face,bindex,1] != 0) ? wm1 : 0;
                    result[i][2] = (adj_side[face,bindex,2] != 0) ? hm1 : 0;
                    result[i][1+adj_side[face,bindex,3]] = yp;

                    break;
                case 2:
                    // wrap in +y direction
                    bindex = 1;

                    result[i][0] = adj_side[face,bindex,0];
                    result[i][1] = (adj_side[face,bindex,1] != 0) ? wm1 : 0;
                    result[i][2] = (adj_side[face,bindex,2] != 0) ? hm1 : 0;
                    result[i][1+adj_side[face,bindex,3]] = xp;

                    break;
                case 4:
                    // wrap in -x direction
                    bindex = 2;

                    result[i][0] = adj_side[face,bindex,0];
                    result[i][1] = (adj_side[face,bindex,1] != 0) ? wm1 : 0;
                    result[i][2] = (adj_side[face,bindex,2] != 0) ? hm1 : 0;
                    result[i][1+adj_side[face,bindex,3]] = yp;

                    break;
                case 8:
                    // wrap in -y direction
                    bindex = 3;

                    result[i][0] = adj_side[face,bindex,0];
                    result[i][1] = (adj_side[face,bindex,1] != 0) ? wm1 : 0;
                    result[i][2] = (adj_side[face,bindex,2] != 0) ? hm1 : 0;
                    result[i][1+adj_side[face,bindex,3]] = xp;

                    break;
                default:
                    // double wrap
                    result[i][0] = -1;
                    result[i][1] = -1;
                    result[i][2] = -1;

                    break;
                }
            }
            return result;
        }

        public void move_active(int dx, int dy, int df)
        {
            int xp = this.act_x + dx;
            int yp = this.act_y + dy;
            int fp = this.act_face + df;

            xp = Math.Max(xp, 0);
            yp = Math.Max(yp, 0);

            xp = (int) Math.Min(xp, this.face_width-1);
            yp = (int) Math.Min(yp, this.face_height-1);

            fp %= 6;
            fp += (fp < 0) ? 6 : 0;

            this.act_x = xp;
            this.act_y = yp;
            this.act_face = fp;

            return;
        }

        public void update_mesh()
        {
            Array.Copy(vertex_base, vertex, vertex.Length);

            // update highlight.
            if (this.show_highlight)
            {
                this.set_color(this.act_face, this.act_x, this.act_y, C_ACTIVE);
                int[][] nbhd = this.get_adj(this.act_face, this.act_x, this.act_y);

                for(int i=0;i<8;i++)
                {
                    int fp = nbhd[i][0];
                    int xp = nbhd[i][1];
                    int yp = nbhd[i][2];
                    if (fp >= 0)
                        this.set_color(fp, xp, yp, C_NBHD);
                }
            }

            // show states
            for(int face=0,i=0;face<6;face++)
            {
                for(int y=0;y<this.face_height;y++)
                {
                    for(int x=0;x<this.face_width;x++,i++)
                    {
                        if (state[i])
                            this.set_color(face, x, y, C_ON);
                    }
                }
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_id[VB.cube_vertex]);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          (IntPtr) (vertex.Length * sizeof(float)),
                          vertex,
                          BufferUsageHint.StreamDraw);

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

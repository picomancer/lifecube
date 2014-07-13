
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

        public App()
        {
            Console.WriteLine("App()");
            this.win = new GameWindow();

            this.win.Load += (sender, e) =>
            {
                // setup settings, load textures, sounds
                this.win.VSync = VSyncMode.On;

                return;
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

                return;
            };

            this.win.RenderFrame += (sender, e) =>
            {
                // render graphics
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);

                GL.Begin(BeginMode.Triangles);

                GL.Color3(Color.MidnightBlue);
                GL.Vertex2(-1.0f, 1.0f);
                GL.Color3(Color.SpringGreen);
                GL.Vertex2(0.0f, -1.0f);
                GL.Color3(Color.Ivory);
                GL.Vertex2(1.0f, 1.0f);

                GL.End();

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
    }
}

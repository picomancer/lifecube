
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
                app.win.Load += (sender, e) =>
                {
                    // setup settings, load textures, sounds
                    app.win.VSync = VSyncMode.On;
                };
 
                app.win.Resize += (sender, e) =>
                {
                    GL.Viewport(0, 0, app.win.Width, app.win.Height);
                };
 
                app.win.UpdateFrame += (sender, e) =>
                {
                    // add game logic, input handling
                    if (app.win.Keyboard[Key.Escape])
                    {
                        app.win.Exit();
                    }
                };
 
                app.win.RenderFrame += (sender, e) =>
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
 
                    app.win.SwapBuffers();
                };
 
                // Run the game at 60 updates per second
                app.win.Run(60.0);
            }
        }
    }
}

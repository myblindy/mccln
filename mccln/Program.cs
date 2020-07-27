using OpenToolkit.Mathematics;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Desktop;
using OpenToolkit.Graphics.OpenGL4;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace mccln
{
    class Program : GameWindow
    {
        public Program() : base(new GameWindowSettings
        {
            RenderFrequency = 60,
            UpdateFrequency = 60,
        }, new NativeWindowSettings
        {
            Profile = ContextProfile.Core,
            API = ContextAPI.OpenGL,
            APIVersion = new Version(4, 6),
            StartFocused = true,
            Size = new Vector2i(800, 600),
            Title = "Shitty Minecraft Clone"
        })
        {
        }

        int cubeShaderProgram, vertexBufferObject, indexBufferObject, vertexArrayObject;
        Quaternion cameraQuaternion = Quaternion.Identity;
        Matrix4 cameraMatrix = Matrix4.Identity;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vertex
        {
            public Vector3 Position, Normal;
            public Color4 Color;

            public static readonly int Size = Marshal.SizeOf<Vertex>();
            public static void SetupVertexArray(int vao)
            {
                GL.EnableVertexArrayAttrib(vao, 0);
                GL.VertexArrayAttribFormat(vao, 0, 3, VertexAttribType.Float, false, 0);
                GL.VertexArrayAttribBinding(vao, 0, 0);

                GL.EnableVertexArrayAttrib(vao, 1);
                GL.VertexArrayAttribFormat(vao, 1, 3, VertexAttribType.Float, false, (int)Marshal.OffsetOf<Vertex>(nameof(Normal)));
                GL.VertexArrayAttribBinding(vao, 1, 0);

                GL.EnableVertexArrayAttrib(vao, 2);
                GL.VertexArrayAttribFormat(vao, 2, 3, VertexAttribType.Float, false, (int)Marshal.OffsetOf<Vertex>(nameof(Color)));
                GL.VertexArrayAttribBinding(vao, 2, 0);
            }
        }

        static readonly Vertex[] vertices = new[]
        {
            new Vertex { Position = new Vector3(0, 1, -2), Normal = new Vector3(0, 0, -1), Color = Color4.Red },
            new Vertex { Position = new Vector3(-1, 0, -2), Normal = new Vector3(0, 0, -1), Color = Color4.Green },
            new Vertex { Position = new Vector3(1, 0, -2), Normal = new Vector3(0, 0, -1), Color = Color4.Blue },
        };

        protected override void OnLoad()
        {
            GL.ClearColor(Color4.Aquamarine);
            GL.Enable(EnableCap.DepthTest);

            // enable debug messages
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            unsafe
            {
                GL.DebugMessageCallback((src, type, id, severity, len, msg, usr) =>
                    Console.WriteLine($"GL ERROR {Encoding.ASCII.GetString((byte*)msg, len)}, type: {type}, severity: {severity}, source: {src}"), IntPtr.Zero);
            }

            // create the shaders
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, File.ReadAllText("Shaders/cube.vert"));
            GL.CompileShader(vertexShader);

            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, File.ReadAllText("Shaders/cube.frag"));
            GL.CompileShader(fragmentShader);

            cubeShaderProgram = GL.CreateProgram();
            GL.AttachShader(cubeShaderProgram, vertexShader);
            GL.AttachShader(cubeShaderProgram, fragmentShader);
            GL.LinkProgram(cubeShaderProgram);

            GL.DetachShader(cubeShaderProgram, vertexShader);
            GL.DetachShader(cubeShaderProgram, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // create the buffers
            GL.CreateBuffers(1, out vertexBufferObject);
            GL.NamedBufferStorage(vertexBufferObject, Vertex.Size * vertices.Length, vertices, BufferStorageFlags.None);

            GL.CreateBuffers(1, out indexBufferObject);
            GL.NamedBufferStorage(indexBufferObject, sizeof(uint) * vertices.Length, new uint[] { 0, 2, 1 }, BufferStorageFlags.None);

            GL.CreateVertexArrays(1, out vertexArrayObject);
            GL.VertexArrayVertexBuffer(vertexArrayObject, 0, vertexBufferObject, IntPtr.Zero, Vertex.Size);
            GL.VertexArrayElementBuffer(vertexArrayObject, vertexBufferObject);

            Vertex.SetupVertexArray(vertexArrayObject);

            base.OnLoad();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);

            // set up the projection matrix
            var projection = Matrix4.CreatePerspectiveFieldOfView(MathF.PI / 2, (float)Size.Y / Size.X, 0.1f, 10f);
            GL.ProgramUniformMatrix4(cubeShaderProgram, 0, false, ref projection);

            base.OnResize(e);
        }

        protected override void OnUnload()
        {
            GL.DeleteProgram(cubeShaderProgram);

            base.OnUnload();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // render the cubes
            GL.UseProgram(cubeShaderProgram);
            GL.UniformMatrix4(4, false, ref cameraMatrix);

            GL.BindVertexArray(vertexArrayObject);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            SwapBuffers();

            base.OnRenderFrame(args);
        }

        static void Main()
        {
            using var program = new Program();
            program.Run();
        }
    }
}

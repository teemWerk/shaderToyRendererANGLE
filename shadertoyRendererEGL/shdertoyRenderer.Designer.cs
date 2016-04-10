using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Reflection;

using System.IO;
using System.Runtime.InteropServices;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Core.Logging;

using WpfGles;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using OpenTK.Platform;
using OpenTK.Platform.Egl;

using WpfGles.Interop;

using FeralTic.DX11;
using FeralTic.DX11.Resources;
using SlimDX.Direct3D11;


namespace VVVV.DX11.Nodes
{
    [PluginInfo(Author = "teem", Name = "Renderer", Category = "WPFEGL", Help = "WPF Widget housing an EGL Context", AutoEvaluate = true, InitialBoxHeight = 120, InitialBoxWidth = 160, InitialComponentMode = TComponentMode.InAWindow, InitialWindowHeight = 300, InitialWindowWidth = 400)]
    public class shadertoyRendererNode : System.Windows.Forms.UserControl, IPluginEvaluate, IPartImportsSatisfiedNotification, IDX11ResourceProvider, IUserInputWindow
    {
        private readonly ElementHost _container = new ElementHost { Dock = System.Windows.Forms.DockStyle.Fill };
        private D3DAngleInterop interop;

        private IntPtr angleSurface;
        private int _program;

        protected bool FInvalidate = false;
        //private bool initTexture = true;

        private string _message = "Null";

        private int uColor;

        // ReSharper disable UnassignedField.Global
        // ReSharper disable MemberCanBePrivate.Global

        [Input("Background Color", DefaultColor = new[] { 0.4, 0.4, 0.4, 1.0 }, IsSingle = true, HasAlpha = false, Order = 1)]
        public IDiffSpread<RGBAColor> ColorIn;

        [Output("Texture")]
        protected Pin<DX11Resource<DX11Texture2D>> FTextureOutput;

        [Output("Height", AsInt = true, IsSingle = true, Order = 1)]
        public ISpread<int> HeightOut;

        [Output("Width", AsInt = true, IsSingle = true, Order = 0)]
        public ISpread<int> WidthOut;

        [Output("Log")]
        public ISpread<string> FOutLog;

        [Output("Is Valid")]
        protected ISpread<bool> FValid;

        [Output("Pointer", IsSingle = true, AsInt = true)]
        protected ISpread<long> FPointer;

        [Import()]
        public ILogger FLogger;

        public shadertoyRendererNode()
        {
           
        }

        public void OnImportsSatisfied()
        {
            FTextureOutput.SliceCount = 1;
            this.FInvalidate = true;
         
            InitializeComponent();
        }

        void InitializeComponent()
        {
            try
            {
                FOutLog[0] = "Angle Interop was successful";

  
            }
            catch
            {
                FOutLog[0] = "Angle Interop init failed";
            }
           
            
            Controls.Clear();
            var grid = new Grid();

            //BitmapImage bmi = new BitmapImage();
            //try
            //{
            //    bmi.BeginInit();
            //    bmi.UriSource = new Uri("C:\\Users/user/Pictures/Saved Pictures/Sunrise-Over-Purple-Clouds-Wallpaper.jpg", UriKind.Absolute);
            //    bmi.EndInit();
                
            //}
            //catch
            //{
            //    //FOutLog[0] = "Couldn't Load Image";
            //}
            //var _image = new System.Windows.Controls.Image();
            //_image.Source = bmi;

            //_image.Stretch = System.Windows.Media.Stretch.Fill;
            //_image.StretchDirection = StretchDirection.Both;
            //grid.Children.Add(_image);
            _container.Child = grid;

            
            Controls.Add(_container);
        }

        // Called when data for any output pin is requested.
        public void Evaluate(int SpreadMax)
        {
            return;
        }

        public IntPtr InputWindowHandle
        {
            get { return _container.Handle; }
        }

        unsafe public void Update(IPluginIO pin, DX11RenderContext context)
        {
            FOutLog[0] = _message;
            

            try
            {
                if (FInvalidate == true)
                {
                    var dummyTex = new DX11Resource<DX11Texture2D>();

                    interop = new D3DAngleInterop();
                    angleSurface = interop.CreateOffscreenSurface(600, 600);
                    //D3DShareHandle =  interop.GetD3DSharedHandleForSurface(angleSurface, 500, 500);
                    // angleSurface = interop.CreateSharedSurfaceFromTexture(v4SharedHandle, 500, 500);

                    IntPtr share = interop.GetD3DSharedHandleForSurface(angleSurface, 500, 500);

                    Texture2D tex = context.Device.OpenSharedResource<Texture2D>(share);
                    ShaderResourceView srv = new ShaderResourceView(context.Device, tex);

                    DX11Texture2D resource = DX11Texture2D.FromTextureAndSRV(context, tex, srv);

                    interop.EnsureContext();

                    var vert = @"//#version 300 es
                        attribute vec4 vPosition;
                        void main()
                        {
                            gl_Position = vPosition;
                        }";

                    var frag = @"//#version 300 es
                        precision mediump float;
                        uniform vec4 color;
                        void main()
                        {
                            gl_FragColor = color;
                        }";


                    var status = new int[1];

                    const int GL_TRUE = 1;

                    _program = GL.CreateProgram();
                    var vs = GL.CreateShader(ShaderType.VertexShader);
                    GL.ShaderSource(vs, vert);
                    GL.CompileShader(vs);

                    GL.GetShader(vs, ShaderParameter.CompileStatus, status);
                    if (status[0] != GL_TRUE)
                    {
                        var error = GL.GetShaderInfoLog(vs);
                        throw new Exception(error);
                    }
                    
                    var fs = GL.CreateShader(ShaderType.FragmentShader);
                    GL.ShaderSource(fs, frag);
                    GL.CompileShader(fs);

                    GL.GetShader(fs, ShaderParameter.CompileStatus, status);
                    if (status[0] != GL_TRUE)
                    {
                        var error = GL.GetShaderInfoLog(fs);
                        throw new Exception(error);
                    }

                    GL.AttachShader(_program, vs);
                    GL.AttachShader(_program, fs);

                    GL.BindAttribLocation(_program, 0, "vPosition");


                    GL.LinkProgram(_program);

                    //GL.DeleteShader(vs);
                    //GL.DeleteShader(fs);

                    GL.UseProgram(_program);


                    GL.GetProgram(_program, GetProgramParameterName.LinkStatus, status);
                    if (status[0] != GL_TRUE)
                    {
                        var error = GL.GetProgramInfoLog(_program);
                        throw new Exception(error);
                    }

                    uColor = GL.GetUniformLocation(_program, "color");

                    FPointer[0] = share.ToInt64();
                    FValid[0] = true;
                    FInvalidate = false;
                }

                interop.MakeCurrent(angleSurface);

                float[] vVertices = new float[] {0.0f, 0.5f, 0.0f,
                 -0.5f, -0.5f, 0.0f,
                 0.5f, -0.5f, 0.0f};
                GL.Viewport(0, 0, 500, 500);
                GL.ClearColor(0.2f, 0.7f, 1.0f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.UseProgram(_program);
                Color4 color = new Color4( (float)ColorIn[0].R, (float)ColorIn[0].G, (float)ColorIn[0].B, (float)ColorIn[0].A);
                GL.Uniform4(uColor, color);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, vVertices);
                GL.EnableVertexAttribArray(0);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

                interop.SwapBuffers(angleSurface);
                GL.Finish();
                _message = "running update";

            }
            catch (Exception e)
            {

                //_message = e.ToString();
            }

            //FOutLog[0] = "Got caught up";

        }

        public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
        {
            for (int i = 0; i < FTextureOutput.SliceCount; i++)
                this.FTextureOutput[i].Dispose(context);
        }
    }
}

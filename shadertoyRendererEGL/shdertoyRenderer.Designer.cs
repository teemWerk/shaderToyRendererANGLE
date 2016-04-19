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

using System.Collections;
using System.Linq;

using System.IO;
using System.Runtime.InteropServices;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Core.Logging;

//using WpfGles;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
//using OpenTK.Graphics.ES20;
using OpenTK.Platform;
using OpenTK.Platform.Egl;

//using WpfGles.Interop;

using FeralTic.DX11;
using FeralTic.DX11.Resources;
using SlimDX.Direct3D11;


namespace VVVV.DX11.Nodes
{
    [PluginInfo(Author = "teem", Name = "Renderer", Category = "WPFEGL", Help = "WPF Widget housing an EGL Context", AutoEvaluate = true, InitialBoxHeight = 120, InitialBoxWidth = 160, InitialComponentMode = TComponentMode.InAWindow, InitialWindowHeight = 300, InitialWindowWidth = 400)]
    public class shadertoyRendererNode : System.Windows.Forms.UserControl, IPluginEvaluate, IPartImportsSatisfiedNotification, IDX11ResourceProvider, IUserInputWindow, IDisposable, IDX11ResourceDataRetriever
    {
        private readonly ElementHost _container = new ElementHost { Dock = System.Windows.Forms.DockStyle.Fill };
        private D3DAngleInterop interop;

        private IntPtr angleSurface;
        private int _program;
        private int[] glTex = new int[4];
        private int mPositionLoc;
        private int mTexCoordLoc;
        private DX11RenderContext shareContext;
        Texture2DDescription desc;

        protected bool FInvalidate = false;

        //private Texture2D tex = null;
        private Texture2D t;
        private SlimDX.DXGI.Resource SharedResource = null;

        private string _message = "Null";

        private int uColor;

        private IntPtr dx11TexHandle;
        // ReSharper disable UnassignedField.Global
        // ReSharper disable MemberCanBePrivate.Global

        [Import()]
        protected IPluginHost FHost;

        [Input("Background Color", DefaultColor = new[] { 0.4, 0.4, 0.4, 1.0 }, IsSingle = true, HasAlpha = false, Order = 1)]
        public IDiffSpread<RGBAColor> ColorIn;

        [Input("Texture In", IsSingle = true)]
        protected Pin<DX11Resource<DX11Texture2D>> FTextureIn;

        [Input("Texture Pointer", IsSingle = true, AsInt = true)]
        protected ISpread<long> FTexturePointer;

        [Output("Texture")]
        public Pin<DX11Resource<DX11Texture2D>> FTextureOutput;

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
            Controls.Clear();
            var grid = new Grid();
            _container.Child = grid;

            
            Controls.Add(_container);
        }

        // Called when data for any output pin is requested.
        public void Evaluate(int SpreadMax)
        {
            if (this.FTextureIn.PluginIO.IsConnected)
            {

                if (this.RenderRequest != null) { this.RenderRequest(this, this.FHost); }

                if (this.AssignedContext == null) { this.SetNull(); return; }

                this.FPointer.SliceCount = SpreadMax;

                shareContext = this.AssignedContext;

                if (this.FTextureIn[0].Contains(shareContext))
                {
                    if (t != null)
                    {
                        this.AssignedContext.CurrentDeviceContext.CopyResource(this.FTextureIn[0][shareContext].Resource, this.t);
                        //_message = "Copying";
                    }
                }

            }
        

                //    try
                //    {
                //        if (this.FTextureIn[0].Contains(context))
                //        {
                //            if (tex != null)
                //            {
                //                Texture2D t = this.FTextureIn[0][context].Resource;

                //                if (t.Description.Width != this.tex.Description.Width
                //                    || t.Description.Height != this.tex.Description.Height
                //                    || t.Description.Format != this.tex.Description.Format)
                //                {
                //                    this.SharedResource.Dispose();
                //                    this.SharedResource = null;
                //                    this.tex.Dispose();
                //                    this.tex = null;
                //                }


                //                if (t.Description.MipLevels > 1)
                //                {
                //                    this.FPointer[0] = 0;
                //                    throw new Exception("Sharing texture with more than one mip level is not allowed");
                //                }
                //                if (t.Description.SampleDescription.Count > 1)
                //                {
                //                    this.FPointer[0] = 0;
                //                    throw new Exception("Sharing multisampled texture is not allowed");
                //                }

                //            }
                //            //Convert texture so it has no mips
                //            if (tex == null)
                //            {

                //                Texture2D t = this.FTextureIn[0][context].Resource;
                //                Texture2DDescription desc = t.Description;
                //                desc.BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget;
                //                desc.OptionFlags = ResourceOptionFlags.Shared;
                //                desc.MipLevels = 1;
                //                this.tex = new Texture2D(context.Device, desc);
                //                this.SharedResource = new SlimDX.DXGI.Resource(this.tex);
                //                this.FPointer[0] = SharedResource.SharedHandle.ToInt64();
                //            }

                //            this.AssignedContext.CurrentDeviceContext.CopyResource(this.FTextureIn[0][context].Resource, this.tex);
                //        }
                //        else
                //        {
                //            this.SetDefault(0);
                //        }
                //    }
                //    catch
                //    {
                //        this.SetDefault(0);
                //    }
                //}
                //else
                //{
                //    this.SetNull();
                //}
                return;
        }

        private void SetNull()
        {
            this.FPointer.SliceCount = 0;
        }

        private void SetDefault(int i)
        {
            this.FPointer[i] = 0;
        }

        public IntPtr InputWindowHandle
        {
            get { return _container.Handle; }
        }

        unsafe public void Update(IPluginIO pin, DX11RenderContext context)
        {
            FOutLog[0] = _message;
            //DX11RenderContext sharedContext = this.AssignedContext;
            try
            {
                if (FInvalidate == true & this.FTextureIn.PluginIO.IsConnected)
                {
                    var dummyTex = new DX11Resource<DX11Texture2D>();

                    interop = new D3DAngleInterop();
                    angleSurface = interop.CreateOffscreenSurface(600, 600);
                    //D3DShareHandle =  interop.GetD3DSharedHandleForSurface(angleSurface, 500, 500);
                    // angleSurface = interop.CreateSharedSurfaceFromTexture(v4SharedHandle, 500, 500);

                    IntPtr share = interop.GetD3DSharedHandleForSurface(angleSurface, 600, 600);

                    Texture2D tex = context.Device.OpenSharedResource<Texture2D>(share);
                    ShaderResourceView srv = new ShaderResourceView(context.Device, tex);

                    DX11Texture2D resource = DX11Texture2D.FromTextureAndSRV(context, tex, srv);
                    SlimDX.DXGI.Resource angleRes = new SlimDX.DXGI.Resource(tex);
                    

                    interop.EnsureContext();
                    //Texture2D tex;
                    SlimDX.DXGI.Resource sharedRes;
                    IntPtr dxHandle = new IntPtr();
                    IntPtr shared;

                    

                    try
                    {


                        //Texture2DDescription desc = tex.Description;
                        //desc.Height = 256;
                        //desc.Width = 256;

                        Texture2D inTex = this.FTextureIn[0][context].Resource;
                        if(inTex == null)
                        {
                            _message = "thiss isn't real";
                        }
                        Texture2DDescription desc, testDesc;
                        try
                        {
                            testDesc = inTex.Description;
                            desc = inTex.Description;
                        }
                        catch(Exception e)
                        {
                            testDesc = tex.Description;
                            desc = tex.Description;
                            _message = e.ToString();
                        }

                        // = inTex.Description;
                        desc.Height = 256;
                        desc.Width = 256;

                        desc.BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget;
                        desc.OptionFlags = ResourceOptionFlags.Shared;
                        desc.MipLevels = 1;

                        if(desc.MipLevels != testDesc.MipLevels)
                        {
                            _message = "MIPS";
                        }

                        if (desc.ArraySize != testDesc.ArraySize)
                        {
                            _message = "Array";
                        }

                        if (desc.CpuAccessFlags != testDesc.CpuAccessFlags)
                        {
                            _message = "CPU";
                        }

                        if (desc.SampleDescription != testDesc.SampleDescription)
                        {
                            _message = "SAMPLE";
                        }

                        if (desc.Usage != testDesc.Usage)
                        {
                            _message = "Usage";
                        }


                        t = new Texture2D(context.Device, desc);

                        sharedRes = new SlimDX.DXGI.Resource(t);
                        dxHandle = sharedRes.SharedHandle;
                        //_message = FTexturePointer[0].ToString();
                        ////FTexturePointer

                        ////int p = unchecked((int)this.FTexturePointer[0]);
                        ////IntPtr shared = new IntPtr(FTexturePointer[0]);

                        //if (shared != null)
                        //{
                        shared =  interop.CreateSharedSurfaceFromTexture(dxHandle, desc.Width, desc.Height);
                        //shared = interop.CreateSharedSurfaceFromTexture(share, 600, 600);
                        //}
                        FTextureOutput[0] = new DX11Resource<DX11Texture2D>();
                        //FTextureOutput[0][context] = t;


                        ShaderResourceView tSRV = new ShaderResourceView(context.Device, t);
                        DX11Texture2D tRes = DX11Texture2D.FromTextureAndSRV(context, t, tSRV);
                        FTextureOutput[0][context] = tRes;


                        GL.GenTextures(1, glTex);
                        GL.BindTexture(TextureTarget.Texture2D, glTex[0]);

                        interop.BindSurfaceToTexture(shared);

                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Nearest);

                        //GL.ActiveTexture(TextureUnit.Texture0);
                        //GL.BindTexture();

                    }
                    catch (Exception e)
                    {
                        //_message = e.ToString();
                    }

                    var vert = @"//#version 300 es
                        attribute vec4 vPosition;
                        attribute vec2 aTexCoord;
                        varying vec2 vTexCoord;
                        void main()
                        {
                            gl_Position = vPosition;
                            vTexCoord = aTexCoord;
                        }";

                    var frag = @"//#version 300 es
                        precision mediump float;
                        varying vec2 vTexCoord;
                        uniform vec4 color;
                        uniform sampler2D iChannel0;
                        void main()
                        {
                            vec2 uv = vTexCoord;
                            vec3 col = texture2D(iChannel0, uv).xyz;
                            gl_FragColor = vec4(col, 1.0);
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
                    mPositionLoc = GL.GetAttribLocation(_program, "vPosition");
                    mTexCoordLoc = GL.GetAttribLocation(_program, "aTexCoord");

                    FPointer[0] = share.ToInt64();
                    FValid[0] = true;
                    FInvalidate = false;
                    
                    //int[] glTex = new int[4];
                    //GL.GenTextures(1, glTex);
                    //GL.TexParameter(GL)
                    //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMagFilter.Nearest);
                }

                //this.AssignedContext.CurrentDeviceContext.CopyResource(this.FTextureIn[0][context].Resource, t);
                context.CurrentDeviceContext.CopyResource(FTextureIn[0][context].Resource, t);

                interop.MakeCurrent(angleSurface);

                float[] vVertices = new float[] {0.0f, 0.5f, 0.0f,
                 -0.5f, -0.5f, 0.0f,
                 0.5f, -0.5f, 0.0f};
                

                float[] vertices = new float[]
                {
                    -0.5f,  0.5f, 0.0f,  // Position 0
                     0.0f,  0.0f,        // TexCoord 0
                    -0.5f, -0.5f, 0.0f,  // Position 1
                     0.0f,  1.0f,        // TexCoord 1
                     0.5f, -0.5f, 0.0f,  // Position 2
                     1.0f,  1.0f,        // TexCoord 2
                     0.5f,  0.5f, 0.0f,  // Position 3
                     1.0f,  0.0f         // TexCoord 3
                };

                short[] indices = { 0, 1, 2, 0, 2, 3 };

                GL.Viewport(0, 0, 500, 500);
                GL.ClearColor((float)ColorIn[0].R, (float)ColorIn[0].G, (float)ColorIn[0].B, 0.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.UseProgram(_program);
                Color4 color = new Color4( (float)ColorIn[0].R, (float)ColorIn[0].G, (float)ColorIn[0].B, (float)ColorIn[0].A);
                GL.Uniform4(uColor, color);

                //Vertex Attributes
                GL.VertexAttribPointer(mPositionLoc, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), vertices);
                // Load the texture coordinate
                GL.VertexAttribPointer(mTexCoordLoc, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), vertices.Skip(3).ToArray());


                //GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, vVertices);
                //GL.EnableVertexAttribArray(0);
                GL.EnableVertexAttribArray(mPositionLoc);
                GL.EnableVertexAttribArray(mTexCoordLoc);

                //Texture Bind
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, glTex[0]);
                

                int iChannel0 = GL.GetUniformLocation(_program, "iChannel0");
                GL.Uniform1(iChannel0, 0);


                GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedShort, indices);

                interop.SwapBuffers(angleSurface);
                GL.Finish();
                //_message = "running update";

            }
            catch (Exception e)
            {

                //_message = e.ToString();
                //_message = e.ToString();
            }

        }

        public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
        {
            for (int i = 0; i < FTextureOutput.SliceCount; i++)
                this.FTextureOutput[i].Dispose(context);
        }

        public DX11RenderContext AssignedContext
        {
            get;
            set;
        }

        public event DX11RenderRequestDelegate RenderRequest;
    }
}

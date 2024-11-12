using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;

namespace Pictomancy.DXDraw;

internal class ClipZone : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Constants
    {
        public Vector2 RenderTargetSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Instance
    {
        public Vector2 Screen;
        public float Alpha;
    }

    public class Data : IDisposable
    {
        public class Builder : IDisposable
        {
            private RenderBuffer<Instance>.Builder _points;

            internal Builder(RenderContext ctx, Data data)
            {
                _points = data._buffer.Map(ctx);
            }

            public void Dispose()
            {
                _points.Dispose();
            }

            public void Add(ref Instance inst) => _points.Add(ref inst);
            public void Add(Vector2 screen, float alpha)
            {
                _points.Add(new Instance()
                {
                    Screen = screen,
                    Alpha = alpha,
                });
            }
        }

        private readonly RenderBuffer<Instance> _buffer;

        public Data(RenderContext ctx, int maxCount, bool dynamic)
        {
            _buffer = new("ClipZone", ctx, maxCount, BindFlags.VertexBuffer, dynamic);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }

        public Builder Map(RenderContext ctx) => new(ctx, this);

        // Draw* should be called after ClipZone.Bind set up its state
        public void DrawSubset(RenderContext ctx, int firstPoint, int numPoints)
        {
            ctx.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_buffer.Buffer, _buffer.ElementSize, 0));
            ctx.Context.Draw(numPoints, firstPoint);
        }

        public void DrawAll(RenderContext ctx) => DrawSubset(ctx, 0, _buffer.CurElements);
    }

    private readonly SharpDX.Direct3D11.Buffer _constantBuffer;
    private readonly InputLayout _il;
    private readonly VertexShader _vs;
    private readonly PixelShader _ps;

    public ClipZone(RenderContext ctx)
    {
        var shader = """
            struct Constants
            {
                float2 renderTargetSize;
            };
            Constants k : register(c0);

            struct VSInput
            {
                float2 screen : SCREEN;
                float alpha : ALPHA;
            };

            struct VSOutput
            {
                float4 projPos : SV_POSITION;
                float alpha : ALPHA;
            };

            VSOutput vs(VSInput z)
            {
                VSOutput vs;
                z.screen.x *= 2 / k.renderTargetSize.x;
                z.screen.y *= 2 / -k.renderTargetSize.y;
                z.screen += float2(-1, 1);

                vs.projPos = float4(z.screen, 0, 1);
                vs.alpha = z.alpha;
                return vs;
            }

            float4 ps(VSOutput input) : SV_TARGET
            {
                return float4(0, 0, 0, input.alpha);
            }
            """;

        var vs = ShaderBytecode.Compile(shader, "vs", "vs_5_0");
        PictoService.Log.Debug($"ClipZone VS compile: {vs.Message}");
        _vs = new(ctx.Device, vs.Bytecode);

        var ps = ShaderBytecode.Compile(shader, "ps", "ps_5_0");
        PictoService.Log.Debug($"ClipZone PS compile: {ps.Message}");
        _ps = new(ctx.Device, ps.Bytecode);

        _constantBuffer = new(ctx.Device, 16 * 2, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        _il = new(ctx.Device, vs.Bytecode,
        [
            new InputElement("SCREEN", 0, Format.R32G32_Float, -1, 0),
            new InputElement("ALPHA", 0, Format.R32_Float, -1, 0),
        ]);
    }

    public void Dispose()
    {
        _constantBuffer.Dispose();
        _il.Dispose();
        _vs.Dispose();
        _ps.Dispose();
    }

    public void UpdateConstants(RenderContext ctx, Constants consts)
    {
        ctx.Context.UpdateSubresource(ref consts, _constantBuffer);
    }

    public void Bind(RenderContext ctx)
    {
        ctx.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        ctx.Context.InputAssembler.InputLayout = _il;
        ctx.Context.VertexShader.Set(_vs);
        ctx.Context.VertexShader.SetConstantBuffer(0, _constantBuffer);
        ctx.Context.PixelShader.Set(_ps);
        ctx.Context.GeometryShader.Set(null);
    }

    public void Draw(RenderContext ctx, Data data)
    {
        Bind(ctx);
        data.DrawAll(ctx);
    }
}

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Pictomancy.DXDraw;

internal class TriFill : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Constants
    {
        public Matrix ViewProj;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Instance
    {
        public Vector3 Point;
        public Vector4 Color;
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
            public void Add(Vector3 world, Vector4 color) =>
                _points.Add(new Instance()
                {
                    Point = world,
                    Color = color
                });
        }

        private readonly RenderBuffer<Instance> _buffer;

        public Data(RenderContext ctx, int maxCount, bool dynamic)
        {
            _buffer = new("Triangle", ctx, maxCount, BindFlags.VertexBuffer, dynamic);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }

        public Builder Map(RenderContext ctx) => new(ctx, this);

        // Draw* should be called after TriFill.Bind set up its state
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

    public TriFill(RenderContext ctx)
    {
        var shader = """
            struct Point
            {
                float3 pos : WORLD;
                float4 color : COLOR;
            };

            struct VSOutput
            {
                float4 projPos : SV_POSITION;
                float4 color : COLOR;
            };

            struct Constants
            {
                float4x4 viewProj;
            };
            Constants k : register(c0);

            VSOutput vs(Point v)
            {
                VSOutput vs;
                vs.projPos = mul(float4(v.pos, 1), k.viewProj);
                vs.color = v.color;
                return vs;
            }

            float4 ps(VSOutput input) : SV_TARGET
            {
                return input.color;
            }
            """;

        var vs = ShaderBytecode.Compile(shader, "vs", "vs_5_0");
        PictoService.Log.Debug($"Point VS compile: {vs.Message}");
        _vs = new(ctx.Device, vs.Bytecode);

        var ps = ShaderBytecode.Compile(shader, "ps", "ps_5_0");
        PictoService.Log.Debug($"Point PS compile: {ps.Message}");
        _ps = new(ctx.Device, ps.Bytecode);

        _constantBuffer = new(ctx.Device, 16 * 4, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        _il = new(ctx.Device, vs.Bytecode,
        [
            new InputElement("WORLD", 0, Format.R32G32B32_Float, -1, 0),
            new InputElement("COLOR", 0, Format.R32G32B32A32_Float, -1, 0),
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
        consts.ViewProj.Transpose();
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

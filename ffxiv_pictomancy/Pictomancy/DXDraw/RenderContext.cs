namespace Pictomancy.DXDraw;

// device + deferred context
internal class RenderContext : IDisposable
{
    public SharpDX.Direct3D11.Device Device { get; private set; }
    public SharpDX.Direct2D1.Device Device2 { get; private set; }

    public SharpDX.Direct3D11.DeviceContext Context { get; private set; }
    //public SharpDX.Direct2D1.DeviceContext Context2 { get; private set; }

    public unsafe RenderContext()
    {
        Device = new((nint)FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance()->D3D11Forwarder);
        Context = new(Device);

        try
        {
            /*
            SharpDX.DXGI.Device dxgiDev = Device.QueryInterfaceOrNull<SharpDX.DXGI.Device>();
            if (dxgiDev != null)
            {
                Device2 = new(dxgiDev);
            }
            */
            using (var dxgiDevice = Device.QueryInterface<SharpDX.DXGI.Device>())
            {
                Device2 = new(dxgiDevice);
            }
        }
        catch (Exception ex)
        {
            //PictoService.Log.Error(ex, "Failed to create D2D device");
        }

        //Context2 = new(Device2, SharpDX.Direct2D1.DeviceContextOptions.None);
    }

    public void Dispose()
    {
        Context.Dispose();
        //Context2.Dispose();
    }

    public void Execute()
    {
        using var cmds = Context.FinishCommandList(true);
        Device.ImmediateContext.ExecuteCommandList(cmds, true);
        Context.ClearState();
    }
}

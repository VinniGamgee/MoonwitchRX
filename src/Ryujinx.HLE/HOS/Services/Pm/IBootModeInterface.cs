using Ryujinx.HLE.HOS.Services.Pm.Types;

namespace Ryujinx.HLE.HOS.Services.Pm
{
    [Service("pm:bm")]
    class IBootModeInterface : IpcService
    {
        public IBootModeInterface(ServiceCtx context) { }

        [CommandCmif(0)]
        // GetBootMode() -> u32
        public ResultCode GetBootMode(ServiceCtx context)
        {
            context.ResponseData.Write((uint)BootMode.Normal);
            return ResultCode.Success;
        }
    }
}

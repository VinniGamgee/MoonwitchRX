namespace ARMeilleure.Translation
{
    public class DelegateInfo
    {
        public nint FuncPtr { get; }

        public DelegateInfo(nint funcPtr)
        {
            FuncPtr = funcPtr;
        }
    }
}

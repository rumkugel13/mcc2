namespace mcc2;

public abstract record StaticInit
{
    public record IntInit(int Value) : StaticInit;
    public record LongInit(long Value) : StaticInit;
    public record UIntInit(uint Value) : StaticInit;
    public record ULongInit(ulong Value) : StaticInit;
    public record DoubleInit(double Value) : StaticInit;
    public record ZeroInit(int Bytes) : StaticInit;
    public record CharInit(int Value) : StaticInit;
    public record UCharInit(int Value) : StaticInit;
    public record StringInit(string Value, bool NullTerminated) : StaticInit;
    public record PointerInit(string Name) : StaticInit;

    private StaticInit() { }
}
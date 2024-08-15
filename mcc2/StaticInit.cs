namespace mcc2;

public abstract record StaticInit
{
    public record IntInit(int Value) : StaticInit;
    public record LongInit(long Value) : StaticInit;
    public record UIntInit(uint Value) : StaticInit;
    public record ULongInit(ulong Value) : StaticInit;
    public record DoubleInit(double Value) : StaticInit;

    private StaticInit() { }
}
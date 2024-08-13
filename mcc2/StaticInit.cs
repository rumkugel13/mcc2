namespace mcc2;

public abstract record StaticInit
{
    public record IntInit(int Value) : StaticInit;
    public record LongInit(long Value) : StaticInit;

    private StaticInit() { }
}
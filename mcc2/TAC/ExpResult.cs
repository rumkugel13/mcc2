namespace mcc2.TAC;

public abstract record ExpResult
{
    public record PlainOperand(Val Val) : ExpResult;
    public record DereferencedPointer(Val Val) : ExpResult;
    public record SubObject(string Base, long Offset) : ExpResult;

    private ExpResult() { }
}
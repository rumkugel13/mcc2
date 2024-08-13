namespace mcc2.TAC;

public abstract record Val
{
    public record Constant(int Value) : Val;
    public record Variable(string Name) : Val;

    private Val() { }
}
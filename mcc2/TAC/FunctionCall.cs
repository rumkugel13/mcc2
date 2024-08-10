namespace mcc2.TAC;

public class FunctionCall : Instruction
{
    public string Identifier;
    public List<Val> Arguments;
    public Val Dst;

    public FunctionCall(string identifier, List<Val> arguments, Val dst)
    {
        this.Identifier = identifier;
        this.Arguments = arguments;
        this.Dst = dst;
    }
}
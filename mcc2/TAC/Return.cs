namespace mcc2.TAC;

public class Return : Instruction
{
    public Val Value;

    public Return(Val value)
    {
        this.Value = value;
    }
}
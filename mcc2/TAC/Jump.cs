namespace mcc2.TAC;

public class Jump : Instruction
{
    public string Identifier;

    public Jump(string identifier)
    {
        this.Identifier = identifier;
    }
}
namespace mcc2.TAC;

public class Label : Instruction
{
    public string Identifier;

    public Label(string identifier)
    {
        this.Identifier = identifier;
    }
}
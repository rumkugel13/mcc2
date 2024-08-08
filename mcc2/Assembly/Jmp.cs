namespace mcc2.Assembly;

public class Jmp : Instruction
{
    public string Identifier;

    public Jmp(string identifier)
    {
        this.Identifier = identifier;
    }
}
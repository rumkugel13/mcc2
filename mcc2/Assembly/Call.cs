namespace mcc2.Assembly;

public class Call : Instruction
{
    public string Identifier;

    public Call(string identifier)
    {
        this.Identifier = identifier;
    }
}
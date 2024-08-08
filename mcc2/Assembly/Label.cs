namespace mcc2.Assembly;

public class Label : Instruction
{
    public string Identifier;

    public Label(string identifier)
    {
        this.Identifier = identifier;
    }
}
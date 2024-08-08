namespace mcc2.Assembly;

public class Pseudo : Operand
{
    public string Identifier;

    public Pseudo(string identifier)
    {
        this.Identifier = identifier;
    }
}
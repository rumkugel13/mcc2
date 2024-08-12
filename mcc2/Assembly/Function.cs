namespace mcc2.Assembly;

public class Function : TopLevel
{
    public string Name;
    public bool Global;
    public List<Instruction> Instructions;

    public Function(string name, bool global, List<Instruction> instructions)
    {
        this.Name = name;
        this.Global = global;
        this.Instructions = instructions;
    }
}
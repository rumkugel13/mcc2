namespace mcc2.TAC;

public class Function : TopLevel
{
    public string Name;
    public bool Global;
    public List<string> Parameters;
    public List<Instruction> Instructions;

    public Function(string name, bool global, List<string> parameters, List<Instruction> instructions)
    {
        this.Name = name;
        this.Global = global;
        this.Parameters = parameters;
        this.Instructions = instructions;
    }
}
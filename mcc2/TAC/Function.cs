namespace mcc2.TAC;

public class Function
{
    public string Name;
    public List<string> Parameters;
    public List<Instruction> Instructions;

    public Function(string name, List<string> parameters, List<Instruction> instructions)
    {
        this.Name = name;
        this.Parameters = parameters;
        this.Instructions = instructions;
    }
}
namespace mcc2.Assembly;

public class Function
{
    public string Name;
    public List<Instruction> Instructions;

    public Function(string name, List<Instruction> instructions)
    {
        this.Name = name;
        this.Instructions = instructions;
    }
}
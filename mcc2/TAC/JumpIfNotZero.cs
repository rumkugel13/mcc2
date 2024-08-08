namespace mcc2.TAC;

public class JumpIfNotZero : Instruction
{
    public Val Condition;
    public string Target;

    public JumpIfNotZero(Val condition, string target)
    {
        this.Condition = condition;
        this.Target = target;
    }
}
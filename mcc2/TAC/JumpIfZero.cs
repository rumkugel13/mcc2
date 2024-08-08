namespace mcc2.TAC;

public class JumpIfZero : Instruction
{
    public Val Condition;
    public string Target;

    public JumpIfZero(Val condition, string target)
    {
        this.Condition = condition;
        this.Target = target;
    }
}
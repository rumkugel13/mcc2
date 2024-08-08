namespace mcc2.Assembly;

public class JmpCC : Instruction
{
    public enum ConditionCode
    {
        E,
        NE,
        G,
        GE,
        L,
        LE
    }

    public ConditionCode Condition;
    public string Identifier;

    public JmpCC(ConditionCode conditionCode, string identifier)
    {
        this.Condition = conditionCode;
        this.Identifier = identifier;
    }
}
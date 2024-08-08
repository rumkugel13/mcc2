namespace mcc2.Assembly;

public class SetCC : Instruction
{
    public JmpCC.ConditionCode Condition;
    public Operand Operand;

    public SetCC(JmpCC.ConditionCode conditionCode, Operand operand)
    {
        this.Condition = conditionCode;
        this.Operand = operand;
    }
}
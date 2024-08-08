namespace mcc2.Assembly;

public class Reg : Operand
{
    public enum RegisterName
    {
        AX,
        R10,
    }

    public RegisterName Register;

    public Reg(RegisterName register)
    {
        this.Register = register;
    }
}
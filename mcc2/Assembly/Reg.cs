namespace mcc2.Assembly;

public class Reg : Operand
{
    public enum RegisterName
    {
        AX,
        DX,
        R10,
        R11,
    }

    public RegisterName Register;

    public Reg(RegisterName register)
    {
        this.Register = register;
    }
}
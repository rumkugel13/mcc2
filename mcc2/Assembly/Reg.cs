namespace mcc2.Assembly;

public class Reg : Operand
{
    public enum RegisterName
    {
        AX,
        CX,
        DX,
        DI,
        SI,
        R8,
        R9,
        R10,
        R11,
    }

    public RegisterName Register;

    public Reg(RegisterName register)
    {
        this.Register = register;
    }
}
namespace mcc2.Types;

public class FunctionType : Type
{
    public int ParameterCount;

    public FunctionType(int parameterCount)
    {
        this.ParameterCount = parameterCount;
    }
}
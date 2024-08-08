namespace mcc2.TAC;

public class Binary : Instruction
{
    public AST.BinaryExpression.BinaryOperator Operator;
    public Val Src1, Src2;
    public Variable Dst;

    public Binary(AST.BinaryExpression.BinaryOperator binaryOperator, Val src1, Val src2, Variable dst)
    {
        this.Operator = binaryOperator;
        this.Src1 = src1;
        this.Src2 = src2;
        this.Dst = dst;
    }
}
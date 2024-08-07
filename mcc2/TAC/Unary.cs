using mcc2.AST;

namespace mcc2.TAC;

public class Unary : Instruction
{
    public UnaryExpression.UnaryOperator UnaryOperator;
    public Val src;
    public Variable dst;

    public Unary(UnaryExpression.UnaryOperator unaryOperator, Val src, Variable dst)
    {
        this.UnaryOperator = unaryOperator;
        this.src = src;
        this.dst = dst;   
    }
}
using System.Diagnostics;

namespace mcc2.AST;

public class ConstantExpression : Expression
{
    public Lexer.Token Constant;

    public ConstantExpression(Lexer.Token constant)
    {
        Debug.Assert(constant.Type == Lexer.TokenType.Constant);
        this.Constant = constant;
    }
}
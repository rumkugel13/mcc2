namespace mcc2.AST;

public abstract record Expression
{
    public enum UnaryOperator
    {
        Complement,
        Negate,
        Not,
        Dereference,
        AddressOf,
    }

    public enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Remainder,
        And,
        Or,
        Equal,
        NotEqual,
        LessThan,
        LessOrEqual,
        GreaterThan,
        GreaterOrEqual
    }

    public record Constant(Const Value, Type Type) : Expression;
    public record Variable(string Identifier, Type Type) : Expression;
    public record Unary(UnaryOperator Operator, Expression Expression, Type Type) : Expression;
    public record Binary(BinaryOperator Operator, Expression ExpressionLeft, Expression ExpressionRight, Type Type) : Expression;
    public record Assignment(Expression ExpressionLeft, Expression ExpressionRight, Type Type) : Expression;
    public record Conditional(Expression Condition, Expression Then, Expression Else, Type Type) : Expression;
    public record FunctionCall(string Identifier, List<Expression> Arguments, Type Type) : Expression;
    public record Cast(Type TargetType, Expression Expression, Type Type) : Expression;
    public record Dereference(Expression Expression) : Expression;
    public record AddressOf(Expression Expression) : Expression;

    private Expression() { }
}
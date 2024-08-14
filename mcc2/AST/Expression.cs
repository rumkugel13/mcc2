namespace mcc2.AST;

public abstract record Expression
{
    public enum UnaryOperator
    {
        Complement,
        Negate,
        Not,
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

    public record ConstantExpression(Const Value, Type Type) : Expression;
    public record VariableExpression(string Identifier, Type Type) : Expression;
    public record UnaryExpression(UnaryOperator Operator, Expression Expression, Type Type) : Expression;
    public record BinaryExpression(BinaryOperator Operator, Expression ExpressionLeft, Expression ExpressionRight, Type Type) : Expression;
    public record AssignmentExpression(Expression ExpressionLeft, Expression ExpressionRight, Type Type) : Expression;
    public record ConditionalExpression(Expression Condition, Expression Then, Expression Else, Type Type) : Expression;
    public record FunctionCallExpression(string Identifier, List<Expression> Arguments, Type Type) : Expression;
    public record CastExpression(Type TargetType, Expression Expression, Type Type) : Expression;

    private Expression() { }
}
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

    public record ConstantExpression(Const Value) : Expression;
    public record VariableExpression(string Identifier) : Expression;
    public record UnaryExpression(UnaryOperator Operator, Expression Expression) : Expression;
    public record BinaryExpression(BinaryOperator Operator, Expression ExpressionLeft, Expression ExpressionRight) : Expression;
    public record AssignmentExpression(Expression ExpressionLeft, Expression ExpressionRight) : Expression;
    public record ConditionalExpression(Expression Condition, Expression Then, Expression Else) : Expression;
    public record FunctionCallExpression(string Identifier, List<Expression> Arguments) : Expression;
    public record CastExpression(Type TargetType, Expression Expression) : Expression;

    private Expression() { }
}
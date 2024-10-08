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
        Increment,
        Decrement,
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
        GreaterOrEqual,
        BitAnd,
        BitOr,
        BitXor,
        BitShiftLeft,
        BitShiftRight,
    }

    public record Constant(Const Value, Type Type) : Expression;
    public record Variable(string Identifier, Type Type) : Expression;
    public record Unary(UnaryOperator Operator, Expression Expression, Type Type) : Expression;
    public record Binary(BinaryOperator Operator, Expression Left, Expression Right, Type Type) : Expression;
    public record Assignment(Expression Left, Expression Right, Type Type) : Expression;
    public record Conditional(Expression Condition, Expression Then, Expression Else, Type Type) : Expression;
    public record FunctionCall(string Identifier, List<Expression> Arguments, Type Type) : Expression;
    public record Cast(Type TargetType, Expression Expression, Type Type) : Expression;
    public record Dereference(Expression Expression, Type Type) : Expression;
    public record AddressOf(Expression Expression, Type Type) : Expression;
    public record Subscript(Expression Left, Expression Right, Type Type) : Expression;
    public record String(string StringVal, Type Type) : Expression;
    public record SizeOf(Expression Expression, Type Type) : Expression;
    public record SizeOfType(Type TargetType, Type Type) : Expression;
    public record Dot(Expression Structure, string Member, Type Type) : Expression;
    public record Arrow(Expression Pointer, string Member, Type Type) : Expression;
    public record CompoundAssignment(BinaryOperator Operator, Expression Left, Expression Right, Type ResultType, Type Type) : Expression;
    public record PostfixIncrement(Expression Expression, Type Type) : Expression;
    public record PostfixDecrement(Expression Expression, Type Type) : Expression;

    private Expression() { }
}
namespace mcc2.AST;

public abstract record Const
{
    public record ConstInt(int Value) : Const;
    public record ConstLong(long Value) : Const;
    public record ConstUInt(uint Value) : Const;
    public record ConstULong(ulong Value) : Const;
    public record ConstDouble(double Value) : Const
    {
        public virtual bool Equals(ConstDouble? other)
        {
            return other != null && BitConverter.DoubleToInt64Bits(this.Value) == BitConverter.DoubleToInt64Bits(other.Value);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    public record ConstChar(int Value) : Const;
    public record ConstUChar(int Value) : Const;

    private Const() { }
}
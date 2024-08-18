namespace mcc2.Assembly;

public abstract record AssemblyType
{
    public record Longword() : AssemblyType;
    public record Quadword() : AssemblyType;
    public record Double() : AssemblyType;
    public record ByteArray(int Size, int Alignment) : AssemblyType;

    private AssemblyType() { }
}
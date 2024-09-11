using mcc2.AST;
using mcc2.Assembly;

namespace mcc2.Tests;

[TestClass]
public class TestChapter01
{
    private string return2Source = """
    int main(void) {
        return 2;
    }
    """;

    private readonly List<Lexer.Token> return2tokens = [
        new Lexer.Token(){Position = 0, End = 3, Type = Lexer.TokenType.IntKeyword},
        new Lexer.Token(){Position = 4, End = 8, Type = Lexer.TokenType.Identifier},
        new Lexer.Token(){Position = 8, End = 9, Type = Lexer.TokenType.OpenParenthesis},
        new Lexer.Token(){Position = 9, End = 13, Type = Lexer.TokenType.VoidKeyword},
        new Lexer.Token(){Position = 13, End = 14, Type = Lexer.TokenType.CloseParenthesis},
        new Lexer.Token(){Position = 15, End = 16, Type = Lexer.TokenType.OpenBrace},
        new Lexer.Token(){Position = 21, End = 27, Type = Lexer.TokenType.ReturnKeyword},
        new Lexer.Token(){Position = 28, End = 29, Type = Lexer.TokenType.IntConstant},
        new Lexer.Token(){Position = 29, End = 30, Type = Lexer.TokenType.Semicolon},
        new Lexer.Token(){Position = 31, End = 32, Type = Lexer.TokenType.CloseBrace},
    ];

    private void ExpectToken(Lexer.Token expect, Lexer.Token got, int i)
    {
        Assert.IsTrue(got.Type == expect.Type, $"Invalid token at {i}, expected: {expect.Type}, got: {got.Type}");
    }

    [TestMethod]
    public void TestLexerReturn2()
    {
        Lexer lexer = new Lexer();
        var list = lexer.Lex(return2Source);

        Assert.IsTrue(list.Count == 10, "Invalid number of tokens produced");

        for (int i = 0; i < list.Count; i++)
        {
            ExpectToken(return2tokens[i], list[i], i);
        }
    }

    private T ExpectType<T>(object node, string type)
    {
        Assert.IsNotNull(node, $"Invalid {type} node");
        Assert.IsInstanceOfType(node, typeof(T), $"Expected {type} type");
        return (T)node;
    }

    [TestMethod]
    public void TestParserReturn2()
    {
        Parser parser = new Parser(return2Source);
        var ast = parser.Parse(return2tokens);

        var program = ExpectType<ASTProgram>(ast, "Program");
        var decls = ExpectType<List<Declaration>>(program.Declarations, "Declarations");
        var funcDecl = ExpectType<Declaration.FunctionDeclaration>(decls[0], "FuncDecl");
        Assert.AreEqual(funcDecl.Identifier, "main", "Invalid Identifier");
        var funcBlock = ExpectType<Block>(funcDecl.Body!, "Block");
        Assert.AreEqual(funcBlock.BlockItems.Count, 1, "Unexpected number of statements");
        var stmt = ExpectType<Statement.ReturnStatement>(funcBlock.BlockItems[0], "ReturnStatemetnt");
        var exp = ExpectType<Expression.Constant>(stmt.Expression!, "ConstantExpression");
        var constInt = ExpectType<Const.ConstInt>(exp.Value, "ConstInt");
        Assert.AreEqual(constInt.Value, 2, "Invalid Constant");
}

    [TestMethod]
    public void TestGeneratorReturn2()
    {
        Parser parser = new Parser(return2Source);
        var ast = parser.Parse(return2tokens);

        Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable = [];
        Dictionary<string, SemanticAnalyzer.StructEntry> typeTable = [];
        new SemanticAnalyzer().Analyze(ast, symbolTable, typeTable);
        TackyEmitter tackyEmitter = new TackyEmitter(symbolTable, typeTable);
        var tacky = tackyEmitter.Emit(ast);
        tacky = new TackyOptimizer().Optimize(tacky, CompilerDriver.Optimizations.EliminateUnreachableCode);

        AssemblyGenerator assemblyGenerator = new AssemblyGenerator(symbolTable,typeTable);
        var assembly = assemblyGenerator.Generate(tacky);

        var program = ExpectType<AssemblyProgram>(assembly, "AssemblyProgram");
        var topLevel = ExpectType<List<TopLevel>>(program.TopLevel, "TopLevel");
        Assert.AreEqual(topLevel.Count, 1, "Unexpected number of toplevel declarations");
        var func = ExpectType<TopLevel.Function>(topLevel[0], "Toplevel.Function");
        Assert.AreEqual(func.Name, "main", "Invalid Identifier");
        Assert.AreEqual(func.Instructions.Count, 3, "Invalid Instruction Count");

        var bin = ExpectType<Instruction.Binary>(func.Instructions[0], "Binary");
        Assert.AreEqual(bin.Operator, Instruction.BinaryOperator.Sub, "Expected Sub operator");
        var dst = ExpectType<Operand.Reg>(bin.Dst, "Reg");
        Assert.AreEqual(dst.Register, Operand.RegisterName.SP, "Expected RSP Register");
        var mov = ExpectType<Instruction.Mov>(func.Instructions[1], "Mov");
        ExpectType<Instruction.Ret>(func.Instructions[2], "Ret");
        var src = ExpectType<Operand.Imm>(mov.Src, "Operand.Imm");
        Assert.AreEqual((long)src.Value, 2, "Invalid Imm value");
    }

    private string assembly = 
@"	.globl main
	.text
main:
	pushq %rbp
	movq %rsp, %rbp
	subq $0, %rsp
	movl $2, %eax
	movq %rbp, %rsp
	popq %rbp
	ret
	.section .note.GNU-stack,"""",@progbits
";

    [TestMethod]
    public void TestEmitterReturn2()
    {
        Parser parser = new Parser(return2Source);
        var ast = parser.Parse(return2tokens);

        Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable = [];
        Dictionary<string, SemanticAnalyzer.StructEntry> typeTable = [];
        new SemanticAnalyzer().Analyze(ast, symbolTable, typeTable);
        TackyEmitter tackyEmitter = new TackyEmitter(symbolTable, typeTable);
        var tacky = tackyEmitter.Emit(ast);
        tacky = new TackyOptimizer().Optimize(tacky, CompilerDriver.Optimizations.EliminateUnreachableCode);
        AssemblyGenerator assemblyGenerator = new AssemblyGenerator(symbolTable, typeTable);
        var assembly = assemblyGenerator.Generate(tacky);
        
        CodeEmitter emitter = new CodeEmitter();
        var code = emitter.Emit(assembly);
        Assert.AreEqual(code.ToString().Length, this.assembly.Length, "Should be same length");
        Assert.AreEqual(code.ToString(), this.assembly, "Should produce the same code");
    }
}
using mcc2.AST;
using mcc2.Assembly;

namespace mcc2.Tests;

[TestClass]
public class TestChapter01
{
    private readonly List<Lexer.Token> return2tokens = [
        new Lexer.Token(){Position = 0, Type = Lexer.TokenType.IntKeyword},
        new Lexer.Token(){Position = 4, Type = Lexer.TokenType.Identifier},
        new Lexer.Token(){Position = 8, Type = Lexer.TokenType.OpenParenthesis},
        new Lexer.Token(){Position = 9, Type = Lexer.TokenType.VoidKeyword},
        new Lexer.Token(){Position = 13, Type = Lexer.TokenType.CloseParenthesis},
        new Lexer.Token(){Position = 15, Type = Lexer.TokenType.OpenBrace},
        new Lexer.Token(){Position = 21, Type = Lexer.TokenType.ReturnKeyword},
        new Lexer.Token(){Position = 28, Type = Lexer.TokenType.Constant},
        new Lexer.Token(){Position = 29, Type = Lexer.TokenType.Semicolon},
        new Lexer.Token(){Position = 31, Type = Lexer.TokenType.CloseBrace},
    ];

    [TestMethod]
    public void TestLexerReturn2()
    {
        string source = File.ReadAllText("../../../Source/return_2.c");
        Lexer lexer = new Lexer();
        var list = lexer.Lex(source);

        Assert.IsTrue(list.Count == 10, "Invalid number of tokens produced");

        for (int i = 0; i < list.Count; i++)
        {
            Lexer.Token token = list[i];
            Assert.IsTrue(token.Type == return2tokens[i].Type, $"Invalid token at {i}, expected: {return2tokens[i].Type}, got: {token.Type}");
        }
    }

    [TestMethod]
    public void TestParserReturn2()
    {
        string source = File.ReadAllText("../../../Source/return_2.c");
        Parser parser = new Parser(source);
        var ast = parser.Parse(return2tokens);

        Assert.IsNotNull(ast, "Invalid Program node");
        Assert.IsNotNull(ast.Function, "Invalid Function node");
        Assert.AreEqual(ast.Function.Name, "main", "Invalid Identifier");
        Assert.IsNotNull(ast.Function.Body, "Invalid Statement");
        Assert.IsInstanceOfType(ast.Function.Body, typeof(ReturnStatement), "Expected ReturnStatement type");
        Assert.IsNotNull(((ReturnStatement)ast.Function.Body).Expression, "Invalid Expression");
        Assert.IsInstanceOfType(((ReturnStatement)ast.Function.Body).Expression, typeof(ConstantExpression), "Expected ConstantExpression type");
        Assert.AreEqual(((ConstantExpression)((ReturnStatement)ast.Function.Body).Expression).Value, 2, "Invalid Constant");
    }

    [TestMethod]
    public void TestGeneratorReturn2()
    {
        string source = File.ReadAllText("../../../Source/return_2.c");
        Parser parser = new Parser(source);
        var ast = parser.Parse(return2tokens);

        TackyEmitter tackyEmitter = new TackyEmitter();
        var tacky = tackyEmitter.Emit(ast);

        AssemblyGenerator assemblyGenerator = new AssemblyGenerator();
        var assembly = assemblyGenerator.Generate(tacky);

        Assert.IsNotNull(assembly, "Invalid Assembly node");
        Assert.IsNotNull(assembly.Function, "Invalid Function node");
        Assert.AreEqual(assembly.Function.Name, "main", "Invalid Identifier");
        Assert.AreEqual(assembly.Function.Instructions.Count, 2, "Invalid Instruction Count");

        Assert.IsInstanceOfType(assembly.Function.Instructions[0], typeof(Mov), "Expected Mov type");
        Assert.IsInstanceOfType(assembly.Function.Instructions[1], typeof(Ret), "Expected Mov type");

        Assert.IsNotNull(((Mov)assembly.Function.Instructions[0]).src, "Invalid src");
        Assert.IsInstanceOfType(((Mov)assembly.Function.Instructions[0]).src, typeof(Imm), "Expected Imm type");
        Assert.AreEqual(((Imm)((Mov)assembly.Function.Instructions[0]).src).Value, 2, "Invalid Imm value");
    }

    private string assembly = 
@"	.globl main
main:
	movl $2,%eax
	ret
	.section .note.GNU-stack,"""",@progbits
";

    [TestMethod]
    public void TestEmitterReturn2()
    {
        string source = File.ReadAllText("../../../Source/return_2.c");
        Parser parser = new Parser(source);
        var ast = parser.Parse(return2tokens);

        TackyEmitter tackyEmitter = new TackyEmitter();
        var tacky = tackyEmitter.Emit(ast);

        AssemblyGenerator assemblyGenerator = new AssemblyGenerator();
        var assembly = assemblyGenerator.Generate(tacky);

        CodeEmitter emitter = new CodeEmitter();
        var code = emitter.Emit(assembly);
        Assert.AreEqual(code.ToString().Length, this.assembly.Length, "Should be same length");
        Assert.AreEqual(code.ToString(), this.assembly, "Should produce the same code");
    }
}
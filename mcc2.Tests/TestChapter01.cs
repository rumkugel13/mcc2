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
        Assert.IsNotNull(ast.Declarations, "Invalid Function node");
        Assert.AreEqual(((FunctionDeclaration)ast.Declarations[0]).Identifier, "main", "Invalid Identifier");
        Assert.IsNotNull(((FunctionDeclaration)ast.Declarations[0]).Body, "Invalid Statement");
        Assert.IsInstanceOfType(((FunctionDeclaration)ast.Declarations[0]).Body, typeof(ReturnStatement), "Expected ReturnStatement type");
        Assert.IsNotNull(((ReturnStatement)((FunctionDeclaration)ast.Declarations[0]).Body.BlockItems[0]).Expression, "Invalid Expression");
        Assert.IsInstanceOfType(((ReturnStatement)((FunctionDeclaration)ast.Declarations[0]).Body.BlockItems[0]).Expression, typeof(ConstantExpression), "Expected ConstantExpression type");
        Assert.AreEqual(((ConstantExpression)((ReturnStatement)((FunctionDeclaration)ast.Declarations[0]).Body.BlockItems[0]).Expression).Value, 2, "Invalid Constant");
    }

    [TestMethod]
    public void TestGeneratorReturn2()
    {
        string source = File.ReadAllText("../../../Source/return_2.c");
        Parser parser = new Parser(source);
        var ast = parser.Parse(return2tokens);

        Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable = [];
        TackyEmitter tackyEmitter = new TackyEmitter(symbolTable);
        var tacky = tackyEmitter.Emit(ast);

        AssemblyGenerator assemblyGenerator = new AssemblyGenerator(symbolTable);
        var assembly = assemblyGenerator.Generate(tacky);

        Assert.IsNotNull(assembly, "Invalid Assembly node");
        Assert.IsNotNull(assembly.TopLevel[0], "Invalid Function node");
        Assert.AreEqual(((Function)assembly.TopLevel[0]).Name, "main", "Invalid Identifier");
        Assert.AreEqual(((Function)assembly.TopLevel[0]).Instructions.Count, 2, "Invalid Instruction Count");

        Assert.IsInstanceOfType(((Function)assembly.TopLevel[0]).Instructions[0], typeof(Mov), "Expected Mov type");
        Assert.IsInstanceOfType(((Function)assembly.TopLevel[0]).Instructions[1], typeof(Ret), "Expected Mov type");

        Assert.IsNotNull(((Mov)((Function)assembly.TopLevel[0]).Instructions[0]).src, "Invalid src");
        Assert.IsInstanceOfType(((Mov)((Function)assembly.TopLevel[0]).Instructions[0]).src, typeof(Imm), "Expected Imm type");
        Assert.AreEqual(((Imm)((Mov)((Function)assembly.TopLevel[0]).Instructions[0]).src).Value, 2, "Invalid Imm value");
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

        Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable = [];
        TackyEmitter tackyEmitter = new TackyEmitter(symbolTable);
        var tacky = tackyEmitter.Emit(ast);

        AssemblyGenerator assemblyGenerator = new AssemblyGenerator(symbolTable);
        var assembly = assemblyGenerator.Generate(tacky);
        
        CodeEmitter emitter = new CodeEmitter(symbolTable);
        var code = emitter.Emit(assembly);
        Assert.AreEqual(code.ToString().Length, this.assembly.Length, "Should be same length");
        Assert.AreEqual(code.ToString(), this.assembly, "Should produce the same code");
    }
}
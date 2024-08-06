using mcc2.AST;

namespace mcc2.Tests;

[TestClass]
public class TestChapter01
{
    private readonly List<Lexer.Token> return2tokens = [
        new Lexer.Token(){Type = Lexer.TokenType.IntKeyword},
        new Lexer.Token(){Type = Lexer.TokenType.Identifier},
        new Lexer.Token(){Type = Lexer.TokenType.OpenParenthesis},
        new Lexer.Token(){Type = Lexer.TokenType.VoidKeyword},
        new Lexer.Token(){Type = Lexer.TokenType.CloseParenthesis},
        new Lexer.Token(){Type = Lexer.TokenType.OpenBrace},
        new Lexer.Token(){Type = Lexer.TokenType.ReturnKeyword},
        new Lexer.Token(){Type = Lexer.TokenType.Constant},
        new Lexer.Token(){Type = Lexer.TokenType.Semicolon},
        new Lexer.Token(){Type = Lexer.TokenType.CloseBrace},
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
        Parser parser = new Parser();
        var ast = parser.Parse(return2tokens);

        Assert.IsNotNull(ast, "Invalid Program node");
        Assert.IsNotNull(ast.Function, "Invalid Function node");
        Assert.IsNotNull(ast.Function.Identifier, "Invalid Identifier");
        Assert.IsNotNull(ast.Function.Body, "Invalid Statement");
        Assert.IsInstanceOfType(ast.Function.Body, typeof(ReturnStatement), "Expected ReturnStatement type");
        Assert.IsNotNull(((ReturnStatement)ast.Function.Body).Expression, "Invalid Expression");
        Assert.IsInstanceOfType(((ReturnStatement)ast.Function.Body).Expression, typeof(ConstantExpression), "Expected ConstantExpression type");
    }
}
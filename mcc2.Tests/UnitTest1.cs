namespace mcc2.Tests;

[TestClass]
public class UnitTest1
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
}
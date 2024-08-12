using System.Text.RegularExpressions;

namespace mcc2
{
    public class Lexer
    {
        public struct Token
        {
            public TokenType Type;
            public int Position;
        }

        public enum TokenType
        {
            Identifier,
            Constant,
            IntKeyword,
            VoidKeyword,
            ReturnKeyword,
            IfKeyword,
            ElseKeyword,
            DoKeyword,
            WhileKeyword,
            ForKeyword,
            BreakKeyword,
            ContinueKeyword,
            StaticKeyword,
            ExternKeyword,
            DoubleHyphen,
            DoubleAmpersand,
            DoubleVertical,
            DoubleEquals,
            ExclamationEquals,
            LessThanEquals,
            GreaterThanEquals,
            OpenParenthesis,
            CloseParenthesis,
            OpenBrace,
            CloseBrace,
            Semicolon,
            Hyphen,
            Tilde,
            Plus,
            Asterisk,
            ForwardSlash,
            Percent,
            Exclamation,
            LessThan,
            GreaterThan,
            Equals,
            Question,
            Colon,
            Comma,
        }

        // note: pattern order needs to match tokentype order
        private readonly string[] patterns = [
            "[a-zA-Z_]\\w*\\b",
            "[0-9]+\\b",
            "int\\b",
            "void\\b",
            "return\\b",
            "if\\b",
            "else\\b",
            "do\\b",
            "while\\b",
            "for\\b",
            "break\\b",
            "continue\\b",
            "static\\b",
            "extern\\b",
            "--",
            "&&",
            "\\|\\|",
            "==",
            "!=",
            "<=",
            ">=",
            "\\(",
            "\\)",
            "{",
            "}",
            ";",
            "-",
            "~",
            "\\+",
            "\\*",
            "\\/",
            "%",
            "!",
            "<",
            ">",
            "=",
            "\\?",
            ":",
            ",",
        ];

        public List<Token> Lex(string source)
        {
            List<Token> tokens = [];
            int pos = 0;

            while (pos < source.Length)
            {
                if (char.IsWhiteSpace(source[pos]))
                {
                    pos++;
                    continue;
                }

                int longest = 0;
                int longestPattern = 0;

                for (int i = 0; i < patterns.Length; i++)
                {
                    Regex regex = new($"\\G{patterns[i]}");
                    Match match = regex.Match(source, pos);
                    if (match.Success)
                    {
                        // note: >= matches keywords of the same length as identifiers afterwards
                        if (match.Length >= longest)
                        {
                            longest = match.Length;
                            longestPattern = i;
                        }
                    }
                }

                if (longest == 0)
                {
                    throw new Exception($"Lexing Error: Invalid Token: {source[pos]}");
                }

                tokens.Add(new Token() { Type = (TokenType)longestPattern, Position = pos });
                pos += longest;
            }

            return tokens;
        }
    }
}
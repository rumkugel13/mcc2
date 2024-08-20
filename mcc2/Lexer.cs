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
            IntConstant,
            LongConstant,
            UnsignedIntConstant,
            UnsignedLongConstant,
            DoubleConstant,
            CharacterConstant,
            StringLiteral,
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
            LongKeyword,
            SignedKeyword,
            UnsignedKeyword,
            DoubleKeyword,
            CharKeyword,
            SizeofKeyword,
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
            Ampersand,
            OpenBracket,
            CloseBracket,
        }

        // note: pattern order needs to match tokentype order
        private readonly string[] patterns = [
            "[a-zA-Z_]\\w*\\b",
            @"([0-9]+)[^\w.]",
            @"([0-9]+[lL])[^\w.]",
            @"([0-9]+[uU])[^\w.]",
            @"([0-9]+([lL][uU]|[uU][lL]))[^\w.]",
            @"(([0-9]*\.[0-9]+|[0-9]+\.?)[Ee][+-]?[0-9]+|[0-9]*\.[0-9]+|[0-9]+\.)[^\w.]",
            """
            '([^'\\\n]|\\['"?\\abfnrtv])'
            """,
            """
            "([^"\\\n]|\\['"\\?abfnrtv])*"
            """,
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
            "long\\b",
            "signed\\b",
            "unsigned\\b",
            "double\\b",
            "char\\b",
            "sizeof\\b",
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
            "&",
            "\\[",
            "\\]",
        ];

        public List<Token> Lex(string source)
        {
            List<Token> tokens = [];
            int pos = 0;
            int line = 0;

            while (pos < source.Length)
            {
                if (char.IsWhiteSpace(source[pos]))
                {
                    if (source[pos] == '\n')
                        line++;
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
                        if (match.Groups.Count > 1 && (i is not (int)TokenType.CharacterConstant and not (int)TokenType.StringLiteral))
                        {
                            if (match.Groups[1].Length >= longest)
                            {
                                longest = match.Groups[1].Length;
                                longestPattern = i;
                            }
                        }
                        // note: >= matches keywords of the same length as identifiers afterwards
                        else if (match.Length >= longest)
                        {
                            longest = match.Length;
                            longestPattern = i;
                        }
                    }
                }

                if (longest == 0)
                {
                    throw new Exception($"Lexing Error: Invalid Token: {source[pos]} at line {line}");
                }

                tokens.Add(new Token() { Type = (TokenType)longestPattern, Position = pos });
                pos += longest;
            }

            return tokens;
        }
    }
}
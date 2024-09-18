using System.Text.RegularExpressions;

namespace mcc2
{
    public class Lexer
    {
        public struct Token
        {
            public TokenType Type;
            public int Position;
            public int End;
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
            StructKeyword,
            GotoKeyword,
            DoubleHyphen,
            DoublePlus,
            DoubleAmpersand,
            DoubleVertical,
            DoubleEquals,
            ExclamationEquals,
            LessThanEquals,
            GreaterThanEquals,
            DoubleLessThan,
            DoubleGreaterThan,
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
            Period,
            HyphenGreaterThan,
            Vertical,
            Caret,
            PlusEquals,
            HyphenEquals,
            AsteriskEquals,
            ForwardSlashEquals,
            PercentEquals,
            AmpersandEquals,
            VerticalEquals,
            CaretEquals,
            DoubleLessThanEquals,
            DoubleGreaterThanEquals,
        }

        // note: pattern order needs to match tokentype order
        private readonly Regex[] patterns = [
            new Regex("\\G[a-zA-Z_]\\w*\\b"),
            new Regex(@"\G([0-9]+)(?![\w.])"),
            new Regex(@"\G([0-9]+[lL])(?![\w.])"),
            new Regex(@"\G([0-9]+[uU])(?![\w.])"),
            new Regex(@"\G([0-9]+([lL][uU]|[uU][lL]))(?![\w.])"),
            new Regex(@"\G(([0-9]*\.[0-9]+|[0-9]+\.?)[Ee][+-]?[0-9]+|[0-9]*\.[0-9]+|[0-9]+\.)(?![\w.])"),
            new Regex(
            """
            \G'([^'\\\n]|\\['"?\\abfnrtv])'
            """
            ),
            new Regex(
            """
            \G"([^"\\\n]|\\['"\\?abfnrtv])*"
            """
            ),
            new Regex("\\Gint\\b"),
            new Regex("\\Gvoid\\b"),
            new Regex("\\Greturn\\b"),
            new Regex("\\Gif\\b"),
            new Regex("\\Gelse\\b"),
            new Regex("\\Gdo\\b"),
            new Regex("\\Gwhile\\b"),
            new Regex("\\Gfor\\b"),
            new Regex("\\Gbreak\\b"),
            new Regex("\\Gcontinue\\b"),
            new Regex("\\Gstatic\\b"),
            new Regex("\\Gextern\\b"),
            new Regex("\\Glong\\b"),
            new Regex("\\Gsigned\\b"),
            new Regex("\\Gunsigned\\b"),
            new Regex("\\Gdouble\\b"),
            new Regex("\\Gchar\\b"),
            new Regex("\\Gsizeof\\b"),
            new Regex("\\Gstruct\\b"),
            new Regex("\\Ggoto\\b"),
            new Regex("\\G--"),
            new Regex("\\G\\+\\+"),
            new Regex("\\G&&"),
            new Regex("\\G\\|\\|"),
            new Regex("\\G=="),
            new Regex("\\G!="),
            new Regex("\\G<="),
            new Regex("\\G>="),
            new Regex("\\G<<"),
            new Regex("\\G>>"),
            new Regex("\\G\\("),
            new Regex("\\G\\)"),
            new Regex("\\G{"),
            new Regex("\\G}"),
            new Regex("\\G;"),
            new Regex("\\G-"),
            new Regex("\\G~"),
            new Regex("\\G\\+"),
            new Regex("\\G\\*"),
            new Regex("\\G\\/"),
            new Regex("\\G%"),
            new Regex("\\G!"),
            new Regex("\\G<"),
            new Regex("\\G>"),
            new Regex("\\G="),
            new Regex("\\G\\?"),
            new Regex("\\G:"),
            new Regex("\\G,"),
            new Regex("\\G&"),
            new Regex("\\G\\["),
            new Regex("\\G\\]"),
            new Regex("\\G\\.(?![0-9])"),
            new Regex("\\G->"),
            new Regex("\\G\\|"),
            new Regex("\\G\\^"),
            new Regex("\\G\\+="),
            new Regex("\\G-="),
            new Regex("\\G\\*="),
            new Regex("\\G/="),
            new Regex("\\G%="),
            new Regex("\\G&="),
            new Regex("\\G\\|="),
            new Regex("\\G\\^="),
            new Regex("\\G<<="),
            new Regex("\\G>>="),
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

                int maxLength = 0;
                int longestPattern = 0;

                for (int i = 0; i < patterns.Length; i++)
                {
                    Match match = patterns[i].Match(source, pos);
                    // note: >= matches keywords of the same length as identifiers afterwards
                    if (match.Success && match.Length >= maxLength)
                    {
                        maxLength = match.Length;
                        longestPattern = i;
                    }
                }

                if (maxLength == 0)
                {
                    throw new Exception($"Lexing Error: Invalid Token: {source[pos]} at line {line}");
                }

                tokens.Add(new Token() { Type = (TokenType)longestPattern, Position = pos, End = pos + maxLength });
                pos += maxLength;
            }

            return tokens;
        }
    }
}
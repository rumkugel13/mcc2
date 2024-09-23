using System.Text.RegularExpressions;

namespace mcc2
{
    public partial class Lexer
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
            SwitchKeyword,
            CaseKeyword,
            DefaultKeyword,
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

        private readonly Regex[] numberPatterns = [
            new Regex(@"\G([0-9]+)(?![\w.])"),
            new Regex(@"\G([0-9]+[lL])(?![\w.])"),
            new Regex(@"\G([0-9]+[uU])(?![\w.])"),
            new Regex(@"\G([0-9]+([lL][uU]|[uU][lL]))(?![\w.])"),
            new Regex(@"\G(([0-9]*\.[0-9]+|[0-9]+\.?)[Ee][+-]?[0-9]+|[0-9]*\.[0-9]+|[0-9]+\.)(?![\w.])"),
        ];

        private readonly Regex charPattern = CharRegex();

        private readonly Regex stringPattern = StringRegex();

        private readonly Dictionary<string, TokenType> keywords = new(){
            { "int", TokenType.IntKeyword },
            { "void", TokenType.VoidKeyword },
            { "return", TokenType.ReturnKeyword },
            { "if", TokenType.IfKeyword },
            { "else", TokenType.ElseKeyword },
            { "do", TokenType.DoKeyword },
            { "while", TokenType.WhileKeyword },
            { "for", TokenType.ForKeyword },
            { "break", TokenType.BreakKeyword },
            { "continue", TokenType.ContinueKeyword },
            { "static", TokenType.StaticKeyword },
            { "extern", TokenType.ExternKeyword },
            { "long", TokenType.LongKeyword },
            { "signed", TokenType.SignedKeyword },
            { "unsigned", TokenType.UnsignedKeyword },
            { "double", TokenType.DoubleKeyword },
            { "char", TokenType.CharKeyword },
            { "sizeof", TokenType.SizeofKeyword },
            { "struct", TokenType.StructKeyword },
            { "goto", TokenType.GotoKeyword },
            { "switch", TokenType.SwitchKeyword },
            { "case", TokenType.CaseKeyword },
            { "default", TokenType.DefaultKeyword },
        };

        private readonly string source;
        private int pos, line;

        public Lexer(string source)
        {
            this.source = source;
            pos = 0;
            line = 1;
        }

        public List<Token> Lex()
        {
            List<Token> tokens = [];

            while (HasMoreTokens())
            {
                var token = source[pos++];
                if (char.IsWhiteSpace(token))
                {
                    if (token == '\n')
                        line++;
                    continue;
                }

                TokenType tokenType;
                int start = pos - 1;
                switch (token)
                {
                    case ';': tokenType = TokenType.Semicolon; break;
                    case '(': tokenType = TokenType.OpenParenthesis; break;
                    case ')': tokenType = TokenType.CloseParenthesis; break;
                    case '{': tokenType = TokenType.OpenBrace; break;
                    case '}': tokenType = TokenType.CloseBrace; break;
                    case '[': tokenType = TokenType.OpenBracket; break;
                    case ']': tokenType = TokenType.CloseBracket; break;
                    case ',': tokenType = TokenType.Comma; break;
                    case ':': tokenType = TokenType.Colon; break;
                    case '?': tokenType = TokenType.Question; break;
                    case '~': tokenType = TokenType.Tilde; break;
                    case '+':
                        if (Match('+'))
                            tokenType = TokenType.DoublePlus;
                        else if (Match('='))
                            tokenType = TokenType.PlusEquals;
                        else
                            tokenType = TokenType.Plus;
                        break;
                    case '-':
                        if (Match('-'))
                            tokenType = TokenType.DoubleHyphen;
                        else if (Match('='))
                            tokenType = TokenType.HyphenEquals;
                        else
                            tokenType = TokenType.Hyphen;
                        break;
                    case '*':
                        if (Match('='))
                            tokenType = TokenType.AsteriskEquals;
                        else
                            tokenType = TokenType.Asterisk;
                        break;
                    case '/':
                        if (Match('='))
                            tokenType = TokenType.ForwardSlashEquals;
                        else
                            tokenType = TokenType.ForwardSlash;
                        break;
                    case '%':
                        if (Match('='))
                            tokenType = TokenType.PercentEquals;
                        else
                            tokenType = TokenType.Percent;
                        break;
                    case '&':
                        if (Match('&'))
                            tokenType = TokenType.DoubleAmpersand;
                        else if (Match('='))
                            tokenType = TokenType.AmpersandEquals;
                        else
                            tokenType = TokenType.Ampersand;
                        break;
                    case '|':
                        if (Match('|'))
                            tokenType = TokenType.DoubleVertical;
                        else if (Match('='))
                            tokenType = TokenType.VerticalEquals;
                        else
                            tokenType = TokenType.Vertical;
                        break;
                    case '^':
                        if (Match('='))
                            tokenType = TokenType.CaretEquals;
                        else
                            tokenType = TokenType.Caret;
                        break;
                    case '<':
                        if (Match('<'))
                        {
                            if (Match('='))
                                tokenType = TokenType.DoubleLessThanEquals;
                            else
                                tokenType = TokenType.DoubleLessThan;
                        }
                        else if (Match('='))
                            tokenType = TokenType.LessThanEquals;
                        else
                            tokenType = TokenType.LessThan;
                        break;
                    case '>':
                        if (Match('>'))
                        {
                            if (Match('='))
                                tokenType = TokenType.DoubleGreaterThanEquals;
                            else
                                tokenType = TokenType.DoubleGreaterThan;
                        }
                        else if (Match('='))
                            tokenType = TokenType.GreaterThanEquals;
                        else
                            tokenType = TokenType.GreaterThan;
                        break;
                    case '!':
                        if (Match('='))
                            tokenType = TokenType.ExclamationEquals;
                        else
                            tokenType = TokenType.Exclamation;
                        break;
                    case '=':
                        if (Match('='))
                            tokenType = TokenType.DoubleEquals;
                        else
                            tokenType = TokenType.Equals;
                        break;
                    case '.':
                        if (char.IsAsciiDigit(Peek()))
                            tokenType = Number(start);
                        else
                            tokenType = TokenType.Period;
                        break;
                    case '\'':
                        tokenType = Character(start);
                        break;
                    case '\"':
                        tokenType = StringLiteral(start);
                        break;
                    default:
                        if (char.IsAsciiDigit(token))
                            tokenType = Number(start);
                        else if (char.IsAsciiLetter(token) || token == '_')
                            tokenType = KeywordOrIdentifier(start);
                        else
                            throw LexError($"Invalid Token: {token} at line {line}");
                        break;
                }

                tokens.Add(new Token() { Type = tokenType, Position = start, End = pos });
            }

            return tokens;
        }

        private TokenType KeywordOrIdentifier(int start)
        {
            while (HasMoreTokens() && (char.IsAsciiLetterOrDigit(source[pos]) || source[pos] == '_'))
                pos++;

            if (keywords.TryGetValue(source[start..pos], out TokenType keyword))
                return keyword;
            else
                return TokenType.Identifier;
        }

        private TokenType Number(int start)
        {
            int maxLength = 0;
            int longestPattern = 0;

            for (int i = 0; i < numberPatterns.Length; i++)
            {
                Match match = numberPatterns[i].Match(source, start);
                if (match.Success && match.Length >= maxLength)
                {
                    maxLength = match.Length;
                    longestPattern = i;
                }
            }

            if (maxLength == 0)
                throw LexError($"Invalid Number constant at line {line}");

            pos += maxLength - 1;
            return (TokenType)longestPattern + (int)TokenType.IntConstant;
        }

        private TokenType Character(int start)
        {
            Match match = charPattern.Match(source, start);
            if (!match.Success)
                throw LexError($"Invalid Character constant at line {line}");
            pos += match.Length - 1;
            return TokenType.CharacterConstant;
        }

        private TokenType StringLiteral(int start)
        {
            Match match = stringPattern.Match(source, start);
            if (!match.Success)
                throw LexError($"Invalid String literal at line {line}");
            pos += match.Length - 1;
            return TokenType.StringLiteral;
        }

        private bool HasMoreTokens()
        {
            return pos < source.Length;
        }

        private bool Match(char token)
        {
            if (Peek() == token)
            {
                pos++;
                return true;
            }

            return false;
        }

        private char Peek()
        {
            if (HasMoreTokens())
            {
                return source[pos];
            }

            return '\0';
        }

        private Exception LexError(string message)
        {
            return new Exception("Lexing Error: " + message);
        }

        [GeneratedRegex("""
            \G'([^'\\\n]|\\['"?\\abfnrtv])'
            """
        )]
        private static partial Regex CharRegex();
        
        [GeneratedRegex("""
            \G"([^"\\\n]|\\['"\\?abfnrtv])*"
            """
        )]
        private static partial Regex StringRegex();
    }
}
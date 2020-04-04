using System;
using System.Text;
using System.IO;
using ILTask.Ast;
using System.Collections.Generic;

namespace ILTask
{
    namespace LanguageParser
    {
        namespace Tokens
        {
            class Token
            {
            }

            class BinaryOperator : Token
            {
                public readonly Ast.BinaryOperator.Operation operation;

                public BinaryOperator(Ast.BinaryOperator.Operation operation)
                {
                    this.operation = operation;
                }
            }

            class UnaryOperator : Token
            {
                public readonly Ast.UnaryOperator.Operation operation;

                public UnaryOperator(Ast.UnaryOperator.Operation operation)
                {
                    this.operation = operation;
                }
            }

            class SetOperator : Token {}
            class LBrack : Token {}
            class RBrack : Token {}
            class Comma : Token {}
            class Semicolon : Token { }
            class LCurlyBrack : Token {}
            class RCurlyBrack : Token {}

            class Name : Token
            {
                public readonly String name;

                public Name(string name)
                {
                    this.name = name;
                }
            }

            class Number : Token
            {
                public readonly long value;

                public Number(long value)
                {
                    this.value = value;
                }
            }

            class Keyword : Token
            {
                public enum Key
                {
                    IFKW,
                    VARKW,
                    FUNCKW,
                    PROCKW,
                    ELSEKW,
                    RETURNKW
                }
                public readonly Key key;

                public Keyword(Key key)
                {
                    this.key = key;
                }
            }
        }
        class SimpleLexer
        {
            public static MType NotNullAssert<MType>(MType arg) where MType : class
            {
                if (arg == null)
                    throw new NullReferenceException("Not null expected");
                return arg;
            }

            private static readonly Dictionary<String, Tokens.Keyword.Key> keywords = new Dictionary<String, Tokens.Keyword.Key>
            {
                {"if", Tokens.Keyword.Key.IFKW},
                {"var", Tokens.Keyword.Key.VARKW},
                {"func", Tokens.Keyword.Key.FUNCKW},
                {"proc", Tokens.Keyword.Key.PROCKW},
                {"else", Tokens.Keyword.Key.ELSEKW},
                {"return", Tokens.Keyword.Key.RETURNKW}
            };

            private MemoryStream stream;
            private int streamPeak, streamAfterPeak;

            private readonly Stack<Tokens.Token> stack = new Stack<Tokens.Token>();

            private int Get()
            {
                var peek = streamPeak;
                streamPeak = streamAfterPeak;
                streamAfterPeak = stream.ReadByte();
                return peek;
            }

            private void SkipWS()
            {
                while (streamPeak == ' ' || streamPeak == '\t' || streamPeak == '\n' || streamPeak == '\r')
                    Get();
            }

            public bool EOF() { return streamPeak == -1; }

            private Tokens.UnaryOperator Unary()
            {
                if (streamPeak == '+')
                {
                    Get();
                    return new Tokens.UnaryOperator(UnaryOperator.Operation.PLS);
                }
                if (streamPeak == '-')
                {
                    Get();
                    return new Tokens.UnaryOperator(UnaryOperator.Operation.NEG);
                }
                return null;
            }

            private Tokens.Token Name()
            {
                if (streamPeak == '_' || streamPeak >= 'a' && streamPeak <= 'z' || streamPeak >= 'A' && streamPeak <= 'Z')
                {
                    StringBuilder builder = new StringBuilder();
                    while (streamPeak == '_' || streamPeak >= 'a' && streamPeak <= 'z' || streamPeak >= 'A' && streamPeak <= 'Z' || streamPeak >= '0' && streamPeak <= '9')
                        builder.Append((char)Get());
                    String name = builder.ToString();
                    Tokens.Keyword.Key key;
                    if (keywords.TryGetValue(name, out key))
                        return new Tokens.Keyword(key);
                    return new Tokens.Name(name);
                }
                return null;
            }

            private Tokens.Token Number()
            {
                if (streamPeak >= '0' && streamPeak <= '9')
                {
                    StringBuilder builder = new StringBuilder();
                    while (streamPeak >= '0' && streamPeak <= '9')
                        builder.Append((char)Get());
                    return new Tokens.Number(Int64.Parse(builder.ToString()));
                }
                return null;
            }

            private Tokens.Token Binary()
            {
                switch (streamPeak)
                {
                    case '+':
                        Get();
                        return new Tokens.BinaryOperator(BinaryOperator.Operation.ADD);
                    case '-':
                        Get();
                        return new Tokens.BinaryOperator(BinaryOperator.Operation.SUB);
                    case '*':
                        Get();
                        return new Tokens.BinaryOperator(BinaryOperator.Operation.MUL);
                    case '/':
                        Get();
                        return new Tokens.BinaryOperator(BinaryOperator.Operation.DIV);
                    case '=':
                        Get();
                        if (streamPeak == '=')
                        {
                            Get();
                            return new Tokens.BinaryOperator(BinaryOperator.Operation.ROE);
                        }
                        return new Tokens.SetOperator();
                    case '<':
                        Get();
                        return new Tokens.BinaryOperator(BinaryOperator.Operation.ROL);
                    case '>':
                        Get();
                        return new Tokens.BinaryOperator(BinaryOperator.Operation.ROG);
                    case '!':
                        if (streamAfterPeak == '=')
                        {
                            Get();
                            Get();
                            return new Tokens.BinaryOperator(BinaryOperator.Operation.RONE);
                        }
                        break;
                    case '&':
                        if (streamAfterPeak == '&')
                        {
                            Get();
                            Get();
                            return new Tokens.BinaryOperator(BinaryOperator.Operation.AND);
                        }
                        break;
                    case '|':
                        if (streamAfterPeak == '|')
                        {
                            Get();
                            Get();
                            return new Tokens.BinaryOperator(BinaryOperator.Operation.OR);
                        }
                        break;
                }
                return null;
            }

            public Tokens.Token LBrack()
            {
                if (streamPeak == '(')
                {
                    Get();
                    return new Tokens.LBrack();
                }
                if (streamPeak == '{')
                {
                    Get();
                    return new Tokens.LCurlyBrack();
                }
                return null;
            }

            public Tokens.Token RBrack()
            {
                if (streamPeak == ')')
                {
                    Get();
                    return new Tokens.RBrack();
                }
                if (streamPeak == '}')
                {
                    Get();
                    return new Tokens.RCurlyBrack();
                }
                return null;
            }

            public Tokens.Token Comma()
            {
                if (streamPeak == ',')
                {
                    Get();
                    return new Tokens.Comma();
                }
                if (streamPeak == ';')
                {
                    Get();
                    return new Tokens.Semicolon();
                }
                return null;
            }

            public SimpleLexer(MemoryStream stream)
            {
                this.stream = stream;
                streamPeak = stream.ReadByte();
                streamAfterPeak = stream.ReadByte();
            }

            private Tokens.Token Process(Func<Tokens.Token>[] processors)
            {
                foreach (var i in processors)
                {
                    var pro = i.Invoke();
                    if (pro != null)
                        return pro;
                }
                return null;
            }

            public Tokens.Token Tokenize()
            {
                if (stack.Count != 0)
                    return stack.Pop();
                while (true)
                {
                    SkipWS();
                    if (streamPeak == '/' && streamAfterPeak == '/')
                    {
                        while (streamPeak != -1 && streamPeak != '\n' && streamPeak != '\r')
                            Get();
                        SkipWS();
                    }
                    else
                        break;
                }
                if (EOF())
                    return null;
                return NotNullAssert(Process(new Func<Tokens.Token>[] { Comma, Binary, Unary, Name, Number, LBrack, RBrack }));
            }

            public void Revert(Tokens.Token token)
            {
                stack.Push(token);
            }
        }
    }
}
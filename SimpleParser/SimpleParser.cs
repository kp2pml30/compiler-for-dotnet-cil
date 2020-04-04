using System;
using System.Collections.Generic;
using System.Linq;
using static ILTask.LanguageParser.SimpleLexer;

namespace ILTask
{
    namespace LanguageParser
    {
        class SimpleParser
        {
            private SimpleLexer lexer;

            private int GetOperatorPriority(Ast.BinaryOperator.Operation op)
            {
                switch (op)
                {
                    case Ast.BinaryOperator.Operation.OR:
                        return 0;
                    case Ast.BinaryOperator.Operation.AND:
                        return 1;
                    case Ast.BinaryOperator.Operation.ROL:
                    case Ast.BinaryOperator.Operation.ROG:
                    case Ast.BinaryOperator.Operation.ROE:
                    case Ast.BinaryOperator.Operation.RONE:
                        return 2;
                    case Ast.BinaryOperator.Operation.ADD:
                    case Ast.BinaryOperator.Operation.SUB:
                        return 3;
                    case Ast.BinaryOperator.Operation.MUL:
                    case Ast.BinaryOperator.Operation.DIV:
                        return 4;
                }
                throw new NotSupportedException("Unkonow operation priority");
            }
            private int GetOpeatorMaxPriority()
            {
                return 5;
            }

            private void Expects<ExpectedType>()
            {
                var next = lexer.Tokenize();
                if (!(next is ExpectedType))
                    throw new InvalidOperationException("Expected " + typeof(ExpectedType).Name);
            }

            private ExpectedType GetExpected<ExpectedType>() where ExpectedType : class
            {
                var next = lexer.Tokenize();
                if (!(next is ExpectedType))
                    throw new InvalidOperationException("Expected " + typeof(ExpectedType).Name);
                return next as ExpectedType;
            }

            private ExpectedType GetExpectedOrNull<ExpectedType>() where ExpectedType : class
            {
                var next = lexer.Tokenize();
                if (next == null)
                    return null;
                if (next is ExpectedType)
                    return next as ExpectedType;
                lexer.Revert(next);
                return null;
            }

            private String[] ParseCommaSeparatedNames()
            {
                var list = new List<String>();
                while (true)
                {
                    var next = lexer.Tokenize();
                    if (!(next is Tokens.Name))
                    {
                        if (list.Count == 0)
                        {
                            lexer.Revert(next);
                            break;
                        }
                        else
                            throw new InvalidOperationException("Bad comma separated list. Expected `Tokens.Name`");
                    }
                    list.Add((next as Tokens.Name).name);

                    var comma = lexer.Tokenize();
                    if (!(comma is Tokens.Comma))
                    {
                        lexer.Revert(comma);
                        break;
                    }
                }
                return list.ToArray();
            }

            private Ast.Expression ParseValue()
            {
                var next = lexer.Tokenize();
                if (next is Tokens.Name)
                {
                    // check for ()
                    var brack = lexer.Tokenize();
                    if (brack is Tokens.LBrack)
                    {
                        var arguments = new List<Ast.Expression>();
                        while (true)
                        {
                            var expr = ParseExpression();
                            if (expr == null)
                                break;
                            arguments.Add(expr);
                            var comma = lexer.Tokenize();
                            if (!(comma is Tokens.Comma))
                            {
                                lexer.Revert(comma);
                                break;
                            }
                        }
                        Expects<Tokens.RBrack>();
                        return new Ast.FunctionCall((next as Tokens.Name).name, arguments.ToArray());
                    }
                    lexer.Revert(brack);
                    return new Ast.Variable((next as Tokens.Name).name);
                }
                else if (next is Tokens.Number)
                    return new Ast.Number((next as Tokens.Number).value);
                else if (next is Tokens.LBrack)
                {
                    var ret = ParseExpression();
                    Expects<Tokens.RBrack>();
                    return ret;
                }
                else if (next is Tokens.BinaryOperator)
                {
                    var asBinaryOperator = next as Tokens.BinaryOperator;
                    if (asBinaryOperator.operation == Ast.BinaryOperator.Operation.ADD)
                        return new Ast.UnaryOperator(NotNullAssert(ParseValue()), Ast.UnaryOperator.Operation.PLS);
                    if (asBinaryOperator.operation == Ast.BinaryOperator.Operation.SUB)
                        return new Ast.UnaryOperator(NotNullAssert(ParseValue()), Ast.UnaryOperator.Operation.NEG);
                }
                lexer.Revert(next);
                return null;
            }

            private Ast.Expression ParseExpression(int depth = 0)
            {
                if (depth == GetOpeatorMaxPriority())
                    return ParseValue();
                var left = ParseExpression(depth + 1);
                if (left == null)
                    return null;
                while (true)
                {
                    var bin = lexer.Tokenize();
                    if (bin == null)
                        break;
                    if (!(bin is Tokens.BinaryOperator))
                    {
                        lexer.Revert(bin);
                        break;
                    }
                    var asBin = bin as Tokens.BinaryOperator;
                    if (GetOperatorPriority(asBin.operation) != depth)
                    {
                        lexer.Revert(bin);
                        break;
                    }
                    var right = ParseExpression(depth + 1);
                    left = new Ast.BinaryOperator(left, NotNullAssert(right), asBin.operation);
                }
                return left;
            }

            private Ast.ReturnStatement ParseReturnStatement()
            {
                var rkw = GetExpectedOrNull<Tokens.Keyword>();
                if (rkw == null)
                    return null;
                if (rkw.key != Tokens.Keyword.Key.RETURNKW)
                {
                    lexer.Revert(rkw);
                    return null;
                }
                var ret = new Ast.ReturnStatement(ParseExpression());
                Expects<Tokens.Semicolon>();
                return ret;
            }

            private Ast.SetStatement ParseSetStatement()
            {
                var name = GetExpectedOrNull<Tokens.Name>();
                if (name == null)
                    return null;
                var setOper = GetExpectedOrNull<Tokens.SetOperator>();
                if (setOper == null)
                {
                    lexer.Revert(name);
                    return null;
                }
                var ret = new Ast.SetStatement(name.name, NotNullAssert(ParseExpression()));
                Expects<Tokens.Semicolon>();
                return ret;
            }

            private Ast.IfStatement ParseIfStatement()
            {
                var next = GetExpectedOrNull<Tokens.Keyword>();
                if (next == null)
                    return null;
                if (next.key != Tokens.Keyword.Key.IFKW)
                {
                    lexer.Revert(next);
                    return null;
                }
                Expects<Tokens.LBrack>();
                var expr = NotNullAssert(ParseExpression());
                Expects<Tokens.RBrack>();
                var ifTrue = NotNullAssert(ParseCurlyBlock());
                Ast.Block ifElse = null;
                var elseKw = GetExpectedOrNull<Tokens.Keyword>();
                if (elseKw != null)
                    if (elseKw.key == Tokens.Keyword.Key.ELSEKW)
                        ifElse = ParseCurlyBlock();
                    else
                        lexer.Revert(elseKw);
                return new Ast.IfStatement(expr, ifTrue, ifElse);
            }

            private Ast.Statement ParseStatement()
            {
                var asReturnStatement = ParseReturnStatement();
                if (asReturnStatement != null)
                    return asReturnStatement;
                var asSetStatement = ParseSetStatement();
                if (asSetStatement != null)
                    return asSetStatement;
                var asIfStatement = ParseIfStatement();
                if (asIfStatement != null)
                    return asIfStatement;

                var expr = ParseExpression();
                if (expr != null)
                {
                    Expects<Tokens.Semicolon>();
                    return new Ast.ExpressionStatement(expr);
                }
                return null;
            }

            private Ast.Block ParseBlock()
            {
                var statements = new List<Ast.Statement>();
                while (true)
                {
                    var statement = ParseStatement();
                    if (statement == null)
                        break;
                    statements.Add(statement);
                }
                return new Ast.Block(statements.ToArray());
            }

            private Ast.Block ParseCurlyBlock()
            {
                Expects<Tokens.LCurlyBrack>();
                var block = ParseBlock();
                Expects<Tokens.RCurlyBrack>();
                return NotNullAssert(block);
            }

            private Ast.FunctionDeclaration ParseFunctionDecl()
            {
                var asKw = GetExpectedOrNull<Tokens.Keyword>();
                if (asKw == null)
                    return null;
                if (asKw.key != Tokens.Keyword.Key.FUNCKW && asKw.key != Tokens.Keyword.Key.PROCKW)
                {
                    lexer.Revert(asKw);
                    return null;
                }
                var name = GetExpected<Tokens.Name>();
                Expects<Tokens.LBrack>();

                // parse arguments
                var arguments = ParseCommaSeparatedNames();

                // get ')' & '{'
                Expects<Tokens.RBrack>();
                Expects<Tokens.LCurlyBrack>();

                // get variables declarations
                var varsKw = lexer.Tokenize();
                String[] locals = new String[0];
                if (varsKw is Tokens.Keyword)
                {
                    var varsKwAsKw = varsKw as Tokens.Keyword;
                    if (varsKwAsKw.key == Tokens.Keyword.Key.VARKW)
                    {
                        locals = ParseCommaSeparatedNames();
                        Expects<Tokens.Semicolon>();
                    }
                    else
                        lexer.Revert(varsKw);
                }
                else
                    lexer.Revert(varsKw);
                // parse block
                var block = ParseBlock();
                // get '}'
                Expects<Tokens.RCurlyBrack>();
                return new Ast.FunctionDeclaration(name.name, arguments.ToArray(), locals.ToArray(), block, asKw.key == Tokens.Keyword.Key.FUNCKW);
            }

            public Ast.Program Parse(SimpleLexer lex)
            {
                if (lexer != null)
                    throw new InvalidOperationException("Multithreading is not supported");
                lexer = lex;

                List<Ast.FunctionDeclaration> functions = new List<Ast.FunctionDeclaration>();
                while (true)
                {
                    var func = ParseFunctionDecl();
                    if (func == null)
                        break;
                    functions.Add(func);
                }
                lexer = null;
                if (!lex.EOF())
                    throw new InvalidOperationException("Bad file : it must be `functionDeclaration*`");
                return new Ast.Program(functions.ToDictionary(x => x.name, x => x));
            }
        }
    }
}

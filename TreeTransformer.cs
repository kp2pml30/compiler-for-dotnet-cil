using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

using System.Collections.Generic;

using System.Linq;

namespace ILTask
{
    class TreeTransformer
    {
        public static Ast.Expression Transform(LangParser.AtomContext ctx)
        {
            var num = ctx.NUMBER();
            if (num != null)
            {
                int ou;
                if (!Int32.TryParse(num.GetText(), out ou))
                    throw new NotSupportedException("Kernel error : can't parse number");
                return new Ast.Number(ou);
            }
            var name = ctx.NAME();
            if (name != null)
                return new Ast.Variable(name.GetText());
            throw new NotSupportedException("Kernel error : unsupported atom");
        }
        public static Ast.Expression Transform(LangParser.ExpressionContext ctx)
        {
            var name = ctx.NAME();
            var exprs = ctx.expression();
            // function call
            if (name != null)
            {
                var expressions = new Ast.Expression[exprs.Length];
                for (uint i = 0; i < exprs.Length; i++)
                    expressions[i] = Transform(exprs[i]);
                return new Ast.FunctionCall(name.GetText(), expressions);
            }
            var atom = ctx.atom();
            if (ctx.LBRACK() != null || atom != null)
            {
                Ast.Expression transformed;
                if (atom != null )
                    transformed = Transform(atom);
                else
                    transformed = Transform(exprs[0]);
                if (ctx.ADD() != null)
                    return new Ast.UnaryOperator(transformed, Ast.UnaryOperator.Operation.PLS);
                if (ctx.SUB() != null)
                    return new Ast.UnaryOperator(transformed, Ast.UnaryOperator.Operation.NEG);
                return transformed;
            }
            if (exprs.Length == 2)
            {
                var mul = ctx.MULPRIOR();
                if (mul != null)
                {
                    var asStr = mul.GetText();
                    if (asStr == "*")
                        return new Ast.BinaryOperator(Transform(exprs[0]), Transform(exprs[1]), Ast.BinaryOperator.Operation.MUL);
                    if (asStr == "/")
                        return new Ast.BinaryOperator(Transform(exprs[0]), Transform(exprs[1]), Ast.BinaryOperator.Operation.DIV);
                }
                var rel = ctx.RELATIONPRIOR();
                if (rel != null)
                {
                    var asStr = rel.GetText();
                    if (asStr == "<")
                        return new Ast.BinaryOperator(Transform(exprs[0]), Transform(exprs[1]), Ast.BinaryOperator.Operation.ROL);
                    if (asStr == ">")
                        return new Ast.BinaryOperator(Transform(exprs[0]), Transform(exprs[1]), Ast.BinaryOperator.Operation.ROG);
                    if (asStr == "==")
                        return new Ast.BinaryOperator(Transform(exprs[0]), Transform(exprs[1]), Ast.BinaryOperator.Operation.ROE);
                    if (asStr == "!=")
                        return new Ast.BinaryOperator(Transform(exprs[0]), Transform(exprs[1]), Ast.BinaryOperator.Operation.RONE);
                    throw new NotSupportedException("Kernel error : unsupported relation type");
                }
                if (ctx.OPAND() != null)
                    return new Ast.BinaryOperator(Transform(exprs[0]), Transform(exprs[1]), Ast.BinaryOperator.Operation.AND);
                if (ctx.OPOR() != null)
                    return new Ast.BinaryOperator(Transform(exprs[0]), Transform(exprs[1]), Ast.BinaryOperator.Operation.OR);
                if (ctx.ADD() != null)
                    return new Ast.BinaryOperator(Transform(exprs[0]), Transform(exprs[1]), Ast.BinaryOperator.Operation.ADD);
                if (ctx.SUB() != null)
                    return new Ast.BinaryOperator(Transform(exprs[0]), Transform(exprs[1]), Ast.BinaryOperator.Operation.SUB);
            }
            throw new NotSupportedException("Kernel error : unsupported expression type");
        }

        public static Ast.SetStatement Transform(LangParser.SetStatementContext ctx)
        {
            return new Ast.SetStatement(ctx.NAME().GetText(), Transform(ctx.expression()));
        }

        public static Ast.Statement Transform(LangParser.StatementContext ctx)
        {
            // {} to hide variables and don't use them by mistake in if statement
            {
                var asSetStatement = ctx.setStatement();
                if (asSetStatement != null)
                    return Transform(asSetStatement);
            }
            {
                var asExpression = ctx.expression();
                if (asExpression != null)
                    return new Ast.ExpressionStatement(Transform(asExpression));
            }
            {
                var asReturn = ctx.returnStatement();
                if (asReturn != null)
                    return Transform(asReturn);
            }
            {
                var asIf = ctx.ifStatement();
                if (asIf != null)
                    return Transform(asIf);
            }
            throw new NotSupportedException("Kernel error : unsupported statement type");
        }

        public static Ast.Block Transform(LangParser.BlockContext ctx)
        {
            return new Ast.Block(ctx.statement().Select(x => Transform(x)).ToArray());
        }

        public static Ast.FunctionDeclaration Transform(LangParser.FunctionDeclarationContext ctx)
        {
            var names = ctx.NAME();
            var variables = ctx.variableDeclaration();

            return new Ast.FunctionDeclaration(names[0].GetText(),
                names.Skip(1).Select(x => x.GetText()).ToArray(),
                variables == null ? new String[0] : ctx.variableDeclaration().NAME().Select(x => x.GetText()).ToArray(),
                Transform(ctx.block()),
                ctx.PROCKW() == null
            );
        }

        public static Ast.ReturnStatement Transform(LangParser.ReturnStatementContext ctx)
        {
            var expression = ctx.expression();
            return new Ast.ReturnStatement(expression == null ? null : Transform(expression));
        }

        public static Ast.IfStatement Transform(LangParser.IfStatementContext ctx)
        {
            var blocks = ctx.block();
            return new Ast.IfStatement(Transform(ctx.expression()), Transform(blocks[0]), ctx.ELSEKW() == null ? null : Transform(blocks[1]));
        }

        public static Ast.Program Transform(LangParser.ProgramContext ctx)
        {
            return new Ast.Program(ctx.functionDeclaration().ToDictionary(fun => fun.NAME()[0].GetText(), fun => Transform(fun)));
        }
    }
}

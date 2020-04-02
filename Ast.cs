using System;
using System.Collections.Generic;
using System.Linq;

namespace ILTask
{
    namespace Ast
    {
        public class Listener<R, A>
        {
            public virtual R Accept(Base element, A data) { return default(R); }
            public virtual R Accept(UnaryOperator element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(BinaryOperator element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(FunctionCall element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(Number element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(Variable element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(ExpressionStatement element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(SetStatement element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(Block element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(FunctionDeclaration element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(ReturnStatement element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(IfStatement element, A data) { return Accept((Base)element, data); }
            public virtual R Accept(Program element, A data) { return Accept((Base)element, data); }
        }

        public abstract class Base
        {
            public abstract R Accept<R, A>(Listener<R, A> listener, A data);
        }

        public abstract class Expression : Base
        {
        }

        public abstract class Statement : Base
        {
        }

        public class ExpressionStatement : Statement
        {
            public Expression expression;

            public ExpressionStatement(Expression expression)
            {
                this.expression = expression;
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class ReturnStatement : Statement
        {
            public Expression expression;

            public ReturnStatement(Expression expression)
            {
                this.expression = expression;
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class SetStatement : Statement
        {
            public String varName;
            public Expression expression;

            public SetStatement(String varName, Expression expression)
            {
                this.varName = varName;
                this.expression = expression;
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class IfStatement : Statement
        {
            public Expression condition;
            public Block ifTrue, ifFalse;

            public IfStatement(Expression condition,  Block ifTrue, Block ifFalse)
            {
                this.condition = condition;
                this.ifTrue = ifTrue;
                this.ifFalse = ifFalse;
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class UnaryOperator : Expression
        {
            public Expression child;
            public enum Operation
            {
                NEG,
                PLS
            }
            public Operation operation;
            public UnaryOperator(Expression child, Operation operation)
            {
                this.child = child;
                this.operation = operation;
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class BinaryOperator : Expression
        {
            public Expression left, right;
            public enum Operation
            {
                OR,
                AND,

                ROL,
                ROG,
                ROE,
                RONE,

                ADD,
                SUB,

                MUL,
                DIV,
            }
            public Operation operation;
            public BinaryOperator(Expression left, Expression right, Operation operation)
            {
                this.left = left;
                this.right = right;
                this.operation = operation;
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class FunctionCall : Expression
        {
            public String name;
            public Expression[] arguments;

            public FunctionCall(String name, Expression[] arguments)
            {
                this.name = name;
                this.arguments = arguments;
            }

            public Type[] GetArgumentTypes()
            {
                return Enumerable.Repeat(typeof(long), arguments.Length).ToArray();
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class Number : Expression
        {
            public readonly long value;
            public Number(long value)
            {
                this.value = value;
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class Variable : Expression
        {
            public readonly String name;
            public Variable(String name)
            {
                this.name = name;
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class Block : Base
        {
            public Statement[] statements;

            public Block(Statement[] statements)
            {
                this.statements = statements;
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class FunctionDeclaration : Base
        {
            public String name;
            public String[] arguments;
            public String[] locals;
            public Block block;
            public bool hasReturn;

            public FunctionDeclaration(String name, String[] arguments, String[] locals, Block block, bool hasReturn = true)
            {
                this.name = name;
                this.arguments = arguments;
                this.locals = locals;
                this.block = block;
                this.hasReturn = hasReturn;
            }

            public Type[] GetArgumentTypes()
            {
                return Enumerable.Repeat(typeof(long), arguments.Length).ToArray();
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }

        public class Program : Base
        {
            public Dictionary<String, FunctionDeclaration> functions = new Dictionary<String, FunctionDeclaration>();

            public Program(Dictionary<string, FunctionDeclaration> functions)
            {
                this.functions = functions;
            }

            public override R Accept<R, A>(Listener<R, A> listener, A data) { return listener.Accept(this, data); }
        }
    }
}

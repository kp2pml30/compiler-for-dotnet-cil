using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using ILTask.Ast;

namespace ILTask
{
    class Compiler
    {
        private Dictionary<String, MethodBuilder> functions;

        public Compiler(Type storage = null)
        {
            this.storage = storage;
        }

        private Type storage;

        private class CompilerListener : Listener<bool, int> // returns true if void was pushed, dummy
        {
            private Compiler parent;
            private ILGenerator il;
            private FunctionDeclaration currentFunction;

            public CompilerListener(Compiler compiler)
            {
                parent = compiler;
            }

            private void EmitI4Negate()
            {
                // is it possible to improve this part?
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.And);
                il.Emit(OpCodes.Conv_I8);
            }

            private void VerifyNotVoid(bool value)
            {
                if (value == true)
                    throw new InvalidOperationException("Expected non vod expression");
            }

            public override bool Accept(Base element, int data)
            {
                throw new NotSupportedException("Kernel error : not supported operation");
            }
            public override bool Accept(Number element, int data)
            {
                il.Emit(OpCodes.Ldc_I8, element.value);
                return false;
            }
            public override bool Accept(UnaryOperator element, int data)
            {
                VerifyNotVoid(element.child.Accept(this, 0));
                switch (element.operation)
                {
                    case UnaryOperator.Operation.PLS:
                        // il.Emit(OpCodes.);
                        break;
                    case UnaryOperator.Operation.NEG:
                        il.Emit(OpCodes.Neg);
                        break;
                    default:
                        throw new NotSupportedException("Kernel error : unknown unary operator");
                }
                return false;
            }
            public override bool Accept(BinaryOperator element, int data)
            {
                VerifyNotVoid(element.left.Accept(this, 0));
                if (element.operation == BinaryOperator.Operation.AND)
                {
                    var endLabel = il.DefineLabel();
                    var endAndPush = il.DefineLabel();
                    /* check inverted
                     * if 0 goto endlabel
                     * otherwise pop and check second
                     * endlabel:
                     */
                    il.Emit(OpCodes.Ldc_I8, 0L);
                    il.Emit(OpCodes.Beq, endAndPush);

                    VerifyNotVoid(element.right.Accept(this, 0));
                    il.Emit(OpCodes.Ldc_I8, 0L);
                    il.Emit(OpCodes.Ceq);
                    EmitI4Negate();
                    il.Emit(OpCodes.Br, endLabel);

                    il.MarkLabel(endAndPush);
                    il.Emit(OpCodes.Ldc_I8, 0L);
                    il.MarkLabel(endLabel);

                    return false;
                }
                if (element.operation == BinaryOperator.Operation.OR)
                {
                    var endLabel = il.DefineLabel();
                    var endAndPush = il.DefineLabel();

                    il.Emit(OpCodes.Ldc_I8, 0L);
                    il.Emit(OpCodes.Bne_Un, endAndPush);

                    VerifyNotVoid(element.right.Accept(this, 0));
                    il.Emit(OpCodes.Ldc_I8, 0L);
                    il.Emit(OpCodes.Ceq);
                    EmitI4Negate();
                    il.Emit(OpCodes.Br, endLabel);

                    il.MarkLabel(endAndPush);
                    il.Emit(OpCodes.Ldc_I8, 1L);
                    il.MarkLabel(endLabel);

                    return false;
                }
                element.right.Accept(this, 0);
                switch (element.operation)
                {
                    case BinaryOperator.Operation.ADD:
                        il.Emit(OpCodes.Add);
                        break;
                    case BinaryOperator.Operation.SUB:
                        il.Emit(OpCodes.Sub);
                        break;
                    case BinaryOperator.Operation.MUL:
                        il.Emit(OpCodes.Mul);
                        break;
                    case BinaryOperator.Operation.DIV:
                        il.Emit(OpCodes.Div);
                        break;
                    case BinaryOperator.Operation.ROE:
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Conv_I8);
                        break;
                    case BinaryOperator.Operation.RONE:
                        il.Emit(OpCodes.Ceq);
                        EmitI4Negate();
                        break;
                    case BinaryOperator.Operation.ROL:
                        il.Emit(OpCodes.Clt);
                        il.Emit(OpCodes.Conv_I8);
                        break;
                    case BinaryOperator.Operation.ROG:
                        il.Emit(OpCodes.Cgt);
                        il.Emit(OpCodes.Conv_I8);
                        break;
                    /* already implemented
                        case BinaryOperator.Operation.AND:
                            il.Emit(OpCodes.Mul);
                            break;
                        case BinaryOperator.Operation.OR:
                            il.Emit(OpCodes.Add);
                            break;
                    */
                    default:
                        throw new NotSupportedException("Kernel error : unknown binary operator");
                }
                return false;
            }
            public override bool Accept(IfStatement element, int data)
            {
                if (element.condition.Accept(this, 0))
                    throw new InvalidOperationException("if (void expression)");
                var elseLabel = il.DefineLabel();
                il.Emit(OpCodes.Brfalse, elseLabel);
                element.ifTrue.Accept(this, 0);
                if (element.ifFalse != null)
                {
                    var endLabel = il.DefineLabel();
                    il.Emit(OpCodes.Br, endLabel);
                    il.MarkLabel(elseLabel);
                    element.ifFalse.Accept(this, 0);
                    il.MarkLabel(endLabel);
                }
                else
                    il.MarkLabel(elseLabel);

                return true;
            }
            public override bool Accept(Variable element, int data)
            {
                int ind = Array.IndexOf(currentFunction.arguments, element.name);
                if (ind >= 0)
                {
                    il.Emit(OpCodes.Ldarg, ind);
                    return false;
                }
                ind = Array.IndexOf(currentFunction.locals, element.name);
                if (ind >= 0)
                {
                    il.Emit(OpCodes.Ldloc, ind);
                    return false;
                }
                il.Emit(OpCodes.Ldsfld, parent.storage.GetField(element.name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public));
                return false;
            }
            public override bool Accept(FunctionCall element, int data)
            {
                foreach (var i in element.arguments)
                    i.Accept(this, 0);

                MethodBuilder triedMethod;
                if (parent.functions.TryGetValue(element.name, out triedMethod))
                {
                    il.Emit(OpCodes.Call, triedMethod);
                    return triedMethod.ReturnType == typeof(void);
                }

                var method = parent.storage.GetMethod(element.name, element.GetArgumentTypes());
                il.Emit(OpCodes.Call, method);
                if (method.ReturnType == typeof(void))
                    return true;
                return false;
            }
            public override bool Accept(ExpressionStatement element, int data)
            {
                return element.expression.Accept(this, data);
            }
            public override bool Accept(SetStatement element, int data)
            {
                if (element.expression.Accept(this, 0))
                    throw new InvalidOperationException("Assign to not returning expression");
                int index;
                index = Array.IndexOf(currentFunction.arguments, element.varName);
                if (index >= 0)
                {
                    if (index == (byte)index)
                        il.Emit(OpCodes.Starg_S, (byte)index);
                    else
                        il.Emit(OpCodes.Starg, index);
                    return true;
                }
                index = Array.IndexOf(currentFunction.locals, element.varName);
                if (index >= 0)
                {
                    il.Emit(OpCodes.Stloc, index);
                    return true;
                }
                il.Emit(OpCodes.Stsfld, parent.storage.GetField(element.varName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public));
                return true;
            }
            public override bool Accept(Ast.Block element, int data)
            {
                foreach (var i in element.statements)
                {
                    if (i.Accept(this, 0) == false)
                        il.Emit(OpCodes.Pop);
                }
                return false;
            }
            public override bool Accept(Ast.FunctionDeclaration element, int data)
            {
                var method = parent.functions[element.name];
                if (il != null)
                    throw new NotSupportedException("Nested fucntions are not supported");
                currentFunction = element;
                il = method.GetILGenerator();
                foreach (var i in element.locals)
                    il.DeclareLocal(typeof(long));
                element.block.Accept(this, data);

                if (!element.hasReturn && !(element.block.statements.Last() is ReturnStatement))
                    il.Emit(OpCodes.Ret);

                if (element.hasReturn && !(element.block.statements.Last() is Ast.ReturnStatement))
                {
                    il.Emit(OpCodes.Ldc_I8, 0L);
                    il.Emit(OpCodes.Ret);
                }

                il = null;
                currentFunction = null;
                return false;
            }
            public override bool Accept(Ast.Program element, int data)
            {
                foreach (var a in element.functions)
                {
                    a.Value.Accept(this, 0);
                }
                return false;
            }
            public override bool Accept(ReturnStatement element, int data)
            {
                if ((element.expression == null) == currentFunction.hasReturn)
                    throw new InvalidOperationException("Return doesn't match with fucntion declaration");
                element.expression?.Accept(this, data);
                il.Emit(OpCodes.Ret);
                return true;
            }
        }

        /* trustful return, dummy */
        private class FunctionsVerificationListener : Listener<bool, int>
        {
            private readonly TypeBuilder typeBuilder;
            private readonly Dictionary<String, MethodBuilder> functions;
            private FunctionDeclaration currentFunction;

            public FunctionsVerificationListener(TypeBuilder typeBuilder, Dictionary<string, MethodBuilder> functions)
            {
                this.typeBuilder = typeBuilder;
                this.functions = functions;
            }

            public override bool Accept(Base element, int data) { return false; }

            public override bool Accept(ReturnStatement element, int data)
            {
                if ((element.expression == null) == currentFunction.hasReturn)
                    throw new InvalidOperationException("Return doesn't match with fucntion declaration");
                return true;
            }

            public override bool Accept(Block element, int data)
            {
                for (uint i = 0; i < element.statements.Length; i++)
                {
                    var trusful = element.statements[i].Accept(this, 0);
                    if (trusful)
                        if (i != element.statements.Length - 1)
                            throw new InvalidOperationException("Unreachable code.");
                        else
                            return true;
                }
                return false;
            }

            public override bool Accept(IfStatement element, int data)
            {
                bool ifTrue = element.ifTrue.Accept(this, 0);
                if (element.ifFalse == null)
                    return false;
                return element.ifFalse.Accept(this, 0) && ifTrue; // anyway execute ifFalse block
            }

            public override bool Accept(FunctionDeclaration element, int data)
            {
                if (functions.ContainsKey(element.name))
                    throw new NotSupportedException("fucntion redefenition/overloading is ont supported");
                if (currentFunction != null)
                    throw new NotSupportedException("Nested functions are not supported");
                currentFunction = element;

                functions[element.name] = typeBuilder.DefineMethod(element.name, MethodAttributes.Public | MethodAttributes.Static, element.hasReturn ? typeof(long) : typeof(void), element.GetArgumentTypes());
                if (!element.block.Accept(this, 0) && element.hasReturn)
                    throw new InvalidOperationException("Not all paths of execution return value");

                currentFunction = null;
                return true;
            }

            public override bool Accept(Ast.Program element, int data)
            {
                foreach (var i in element.functions)
                    i.Value.Accept(this, 0);
                return false;
            }
        }

        public Type Compile(Ast.Program program)
        {
            if (functions != null)
                throw new InvalidOperationException("Compiler must finish compilation of first source before compiling other");
            functions = new Dictionary<String, MethodBuilder>();


            var aName = new AssemblyName("DynamicAssemblyCompiler");
            var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
            var typeBuilder = mb.DefineType("ProgramBody", TypeAttributes.Public);

            var verifier = new FunctionsVerificationListener(typeBuilder, functions);
            verifier.Accept(program, 0);

            var listener = new CompilerListener(this);
            listener.Accept(program, 0);

            functions = null;
            return typeBuilder.CreateType();
        }
    }
}

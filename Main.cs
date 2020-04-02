using System;

using Antlr4.Runtime;

using Antlr4.Runtime.Misc;

namespace ILTask
{
    public class Main
    {
        public class ExecutionContext
        {
            public static long bbb = 30;
            public static void Print(long val)
            {
                Console.WriteLine(val);
            }
            public static void EndSection()
            {
                Console.WriteLine("_____________________");
            }
        }

        private class ThrowingErrorListener : BaseErrorListener
        {
            public override void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
            {
                throw new ParseCanceledException("line " + line + ":" + charPositionInLine + " " + msg);
            }
        }
        static void Main(string[] args)
        {
            var parser = new LangParser(new CommonTokenStream(new LangLexer(new AntlrInputStream(@"
func Get1() {
    return 1;
}
proc Test() {
    Print(5);
}
proc PrintAddMul(x, y, z) {
    Print(x + y * z);
}
proc TestRet(x) {
    if (x) {
        return;
    } else {
        // return; // unreachable code
    }
    return;
}
func TestRet2(x) {
    if (x) {
        return 1;
    } else {
        // return; // unreachable code
    }
    return 3; // must have
}
func PrintAndGet(p, r) {
    Print(p);
    return r;
}
proc TestAndOr() {
    PrintAndGet(30, 0) && PrintAndGet(30, 1); // 30
    PrintAndGet(31, 1) && PrintAndGet(31, 1); // 31 31
    PrintAndGet(32, 1) || PrintAndGet(32, 1); // 32
    PrintAndGet(33, 0) || PrintAndGet(33, 1); // 33 33
}
proc Main(a, b, c) {
    var mvar;
    mvar = 11;
    Test();
    Print(bbb + a);
    a = 5;
    Get1();
    PrintAddMul(a + b, Get1(), mvar);
    EndSection();
    Print(1 < 2);
    Print(1 < 0);
    Print(1 == 2);
    Print(1 == 1);
    if (2) {
        Print(333);
    } else {
        Print(222);
    }
    if (0) {
        Print(18);
    } else {
        Print(81);
    }
    EndSection();
    TestAndOr();
    EndSection();
}
"))));
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowingErrorListener());
            var tree = TreeTransformer.Transform(parser.program());
            var compiler = new Compiler(typeof(ExecutionContext));
            var result = compiler.Compile(tree);
            result.GetMethod("Main").Invoke(null, new object[] { 1, 2, 3 });
            Console.WriteLine();
        }
    }
}

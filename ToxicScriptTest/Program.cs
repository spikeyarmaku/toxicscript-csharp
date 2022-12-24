﻿// See https://aka.ms/new-console-template for more information

using ToxicScriptNet;

if (args.Length < 1) {
    var progname = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
    Console.WriteLine(progname + " reads a .txc file an interprets it.");
    Console.WriteLine("USAGE:");
    Console.WriteLine(progname + " filepath");
    return;
}

var filename = args[0];

var sr = new StreamReader(filename);
var str = sr.ReadToEnd();

var tree = ToxicScript.Parse(str);

var env = GlobalEnv<object>.MkGlobalEnv((x) => x, (x) => (float)x, (x) => x.ToString());

var v = ToxicScript.EvalExpr<object>(env, tree);
Console.WriteLine("Evaluating file:\n\n===\n");
switch (v) {
    case Val<object> o:
        Console.WriteLine("Value: " + o.Data.ToString());
        break;
    case Var<object> o:
        Console.WriteLine("Value: " + o.Expr.ToString());
        break;
    default:
        Console.WriteLine("Abstraction");
        break;
}

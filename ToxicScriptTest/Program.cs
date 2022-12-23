// See https://aka.ms/new-console-template for more information

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
if (v is Val<object>) {
    var d = ((Val<object>)v).Data;
    Console.WriteLine(d.ToString());
}

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

var lambdaTr = new Transform<object>((env, prms) => {
    if (prms.Count < 2) {
        throw new InvalidOperationException("Not enough arguments in `lambda`");
    }
    var name = prms[0];
    var body = prms[1];

    return new Transform<object>((dynEnv, prmsInner) => {
        if (prmsInner.Count < 1) {
            throw new InvalidOperationException("Not enough arguments in application of `lambda`");
        }

        var v = ToxicScript.Eval(dynEnv, prmsInner[0]);
        return ToxicScript.Eval(env.Extend(name, v), body);
    });
});

// (let name value body) --> ((lambda name body) value)
var letTr = new Transform<object>((env, prms) => {
    if (prms.Count < 3) {
        throw new InvalidOperationException("Not enough arguments in `let`");
    }
    var name = prms[0];
    var value = prms[1];
    var body = prms[2];

    return ToxicScript.Eval(env,
        new List(new List<Expr>(){new List(new List<Expr>(){
            new Symbol("lambda"), name, body}), value}));
});

var letrecTr = new Transform<object>((env, prms) => {
    if (prms.Count < 3) {
        throw new InvalidOperationException("Not enough arguments in `let`");
    }
    var name = prms[0];
    var value = prms[1];
    var body = prms[2];

    var newEnv = env.Extend(name, new Promise<object>(env, value));
    var newVal = ToxicScript.Eval(newEnv, value);
    newEnv.SetMapping(env.Extend(name, newVal).Lookup);

    return ToxicScript.Eval(newEnv, body);
});

Func<Expr, Value<object>> consTr = (cr) => new Transform<object>((env, prms) => {
    var list = new List<Expr>();
    list.Add(cr);
    list.AddRange(prms);
    return new Promise<object>(env, new List(list));
});

var eqTr = new Transform<object>((env, prms) => {
    if (prms.Count < 2) {
        throw new InvalidOperationException("Not enough arguments in `=`");
    }

    var v1 = ToxicScript.Eval(env, prms[0]);
    var v2 = ToxicScript.Eval(env, prms[1]);

    var v1data = v1 is Opaque<object> ? ((Opaque<object>)v1).Data : null;
    var v2data = v2 is Opaque<object> ? ((Opaque<object>)v2).Data : null;

    if (v1data == null || v2data == null) {
        throw new InvalidOperationException("One of the arguments doesnt evaluate in mathOp");
    }

    return new Opaque<object>((
                v1.GetType().Equals(v2.GetType()) && v1 is Opaque<object> &&
                v1data.Equals(v2data)));
});

var mathTr = (Func<float, float, float> f) => new Transform<object>((env, prms) => {
    if (prms.Count < 2) {
        throw new InvalidOperationException("Not enough arguments in `=`");
    }

    var v1 = ToxicScript.Eval(env, prms[0]);
    var v2 = ToxicScript.Eval(env, prms[1]);

    var v1data = v1 is Opaque<object> ? ((Opaque<object>)v1).Data : null;
    var v2data = v2 is Opaque<object> ? ((Opaque<object>)v2).Data : null;

    if (v1data == null || v2data == null) {
        throw new InvalidOperationException("One of the arguments doesnt evaluate in mathOp");
    }

    if (v1data != null && v2data != null) {
        var n1 = (float)v1data;
        var n2 = (float)v2data;
        return new Opaque<object>(f(n1, n2));
    } else {
        throw new InvalidOperationException("Invalid arguments in math operation");
    }
});

var ifTr = new Transform<object>((env, prms) => {
    if (prms.Count < 3) {
        throw new InvalidOperationException("Not enough arguments in `if`");
    }
    var cond = prms[0];
    var ifTrue = prms[1];
    var ifFalse = prms[2];

    var val = ToxicScript.Eval(env, cond);
    if (val is Opaque<object>) {
        var valData = ((Opaque<object>)val).Data;
        if (valData == null) {
            throw new InvalidOperationException("Invalid condition in `if`");
        }
        if ((bool)valData == true) {
            return ToxicScript.Eval(env, ifTrue);
        } else {
            return ToxicScript.Eval(env, ifFalse);
        }
    } else {
        throw new InvalidOperationException("Invalid condition in `if`");
    }
});

Func<Expr, Value<object>> nthTr = (cr) => new Transform<object>((env, prms) => {
    if (prms.Count < 2) {
        throw new InvalidOperationException("Not enough arguments in `nth`");
    }

    var n = ToxicScript.Eval(env, prms[0]);
    var lst = ToxicScript.Eval(env, prms[1]);

    float d = 0;
    if (n is Opaque<object>) {
        d = (float)((Opaque<object>)n).Data;
    } else {
        throw new InvalidOperationException("First argument does not evaluate to a number");
    }

    if (lst is Promise<object>) {
        var p = (Promise<object>)lst;
        var newEnv = p.Env;
        if (p.Expr is List) {
            var elems = (List)p.Expr;
            var head = elems.GetHead();
            var tail = elems.GetTail();
            if (head == null) {
                throw new InvalidOperationException("Not enough elements in list");
            }
            if (tail.Count == 0) {
                throw new InvalidOperationException("Not enough elements in list");
            } else {
                if (d == 0) {
                    return ToxicScript.Eval(newEnv, tail.First());
                } else {
                    // (nth n (cons elem1 rest)) --> (nth (- n 1) rest))
                    var newElems = new List<Expr>();
                    newElems.Add(cr);
                    newElems.Add(new Symbol((d - 1).ToString()));
                    newElems.AddRange(tail.Skip(1).ToList());
                    var newExpr = new List(newElems);
                    return ToxicScript.Eval(newEnv, newExpr);
                }
            }
        } else {
            throw new InvalidOperationException("Second argument is not a list");
        }
    } else {
        throw new InvalidOperationException("Second argument cannot be evaluated");
    }
});

var add = (float x1, float x2) => x1 + x2;
var sub = (float x1, float x2) => x1 - x2;
var mul = (float x1, float x2) => x1 * x2;

var env = Env<Value<object>>.Default((x) => new Opaque<object>(x),
                                     (x) => new Opaque<object>(x))
    .Extend("lambda",   lambdaTr)
    .Extend("let",      letTr)
    .Extend("letrec",   letrecTr)
    .Extend("=",        eqTr)
    .Extend("+",        mathTr(add))
    .Extend("-",        mathTr(sub))
    .Extend("*",        mathTr(mul))
    .Extend("if",       ifTr)
    .Extend("cons",     consTr)
    .Extend("nth",      nthTr);

var v = ToxicScript.Eval<object>(env, tree);
Console.WriteLine("Evaluating file:\n\n===\n");
if (v is Opaque<object>) {
    var d = ((Opaque<object>)v).Data;
    Console.WriteLine(d.ToString());
}

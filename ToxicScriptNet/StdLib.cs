namespace ToxicScriptNet;

public static class GlobalEnv<T> {
    public static Env<Term<T>> MkGlobalEnv(
        Func<float, T> numToValue,
        Func<T, float> valueToNum,
        Func<string, T> stringToValue) {
        var env = Env<Term<T>>.Default(
            (v) => new Val<T>(numToValue(v)),
            (v) => new Val<T>(stringToValue(v)));
        return
            env
            .Extend(new Symbol("lambda"), LambdaV)
            .Extend(new Symbol("let"), LetV)
            .Extend(new Symbol("letrec"), LetrecV)
            .Extend(new Symbol("+"), MathV(valueToNum, numToValue, (x, y) => x + y))
            .Extend(new Symbol("-"), MathV(valueToNum, numToValue, (x, y) => x - y))
            .Extend(new Symbol("*"), MathV(valueToNum, numToValue, (x, y) => x * y))
            .Extend(new Symbol("/"), MathV(valueToNum, numToValue, (x, y) => x / y))
            .Extend(new Symbol("="), EqV);
    }

    static Term<T> LamF(
        Env<Term<T>> staticEnv,
        Env<Term<T>> dynEnv,
        Expr name,
        Expr body,
        Expr value)
    {
        var val = ToxicScript.EvalExpr(dynEnv, value);
        var newEnv = staticEnv.Extend(name, val);
        return ToxicScript.EvalExpr(newEnv, body);
    }

    static Term<T> LetF(
        Env<Term<T>> staticEnv,
        Env<Term<T>> dynEnv,
        Expr name,
        Expr value,
        Expr body)
    {
        return LamF(staticEnv, dynEnv, name, body, value);
    }

    static Term<T> LambdaV =
        new Abs<T>((_, name) =>
            new Abs<T>((staticEnv, body) =>
                new Abs<T>((dynEnv, value) => {
                    return LamF(staticEnv, dynEnv, name, body, value);})));

    static Term<T> LetV =
        new Abs<T>((_, name) =>
            new Abs<T>((staticEnv, value) =>
                new Abs<T>((dynEnv, body) => {
                    return LetF(staticEnv, dynEnv, name, value, body);})));

    // TODO Test it
    static Term<T> LetrecV =
        new Abs<T>((_, name) =>
            new Abs<T>((staticEnv, value) =>
                new Abs<T>((dynEnv, body) => {
                    var newEnv = dynEnv.Extend(name, new Var<T>(name));
                    var newVal = ToxicScript.EvalExpr(newEnv, value);
                    newEnv = dynEnv.Extend(name, newVal);
                    return ToxicScript.EvalExpr(newEnv, body);
                })));

    // Term<T> LetsV = ...

    // Term<T> LetrecsV =
    //     new Abs<T>((_, defs) => {
    //         if (defs is List) {
    //             List pairs = (List)defs;
    //             return new Abs<T>((dynEnv, body) => {
    //                 var newEnv = dynEnv;
    //                 foreach (Expr p in pairs.Items) {
    //                     if (p is List) {
    //                         var binding = (List)p;
    //                         if (binding.Items.Count != 2) {
    //                             throw new InvalidOperationException("Invalid binding in letrec");    
    //                         } else {
    //                             var name = binding.Items[0];
    //                             var valueE = binding.Items[1];
    //                             var value = ToxicScript.EvalExpr(newEnv, valueE);
    //                             newEnv = newEnv.Extend(name, value);
    //                         }
    //                     } else {
    //                         throw new InvalidOperationException("Invalid binding in letrec");
    //                     }
    //                 }
    //                 return ToxicScript.EvalExpr(newEnv, body);
    //             });
    //         } else {
    //             throw new InvalidOperationException("Invalid binding list in letrec");
    //         }
    //     });

    static Term<T> MathV(Func<T, float> toNum, Func<float, T> fromNum, Func<float, float, float> op) {
        return new Abs<T>((_, e1) =>
            new Abs<T>((env, e2) => {
                var n1 = ToxicScript.EvalExpr(env, e1);
                var n2 = ToxicScript.EvalExpr(env, e2);
                if (n1 is Val<T> && n2 is Val<T>) {
                    return new Val<T>(fromNum(op(toNum(((Val<T>)n1).Data), toNum(((Val<T>)n2).Data))));
                } else {
                    throw new InvalidOperationException("An argument doesn't evaluate to a value");
                }
            }));
    }

    static Term<T> TrueV = new Abs<T>((_, x) => new Abs<T>((env, _) => ToxicScript.EvalExpr(env, x)));
    static Term<T> FalseV = new Abs<T>((_, _) => new Abs<T>((env, y) => ToxicScript.EvalExpr(env, y)));

    static Term<T> EqV =
        new Abs<T>((_, e1) =>
            new Abs<T>((env, e2) => {
                var v1 = ToxicScript.EvalExpr(env, e1);
                var v2 = ToxicScript.EvalExpr(env, e2);
                if (v1 is Val<T> val1 && v2 is Val<T> val2) {
                    if (val1.Data!.Equals(val2.Data)) {
                        return TrueV;
                    } else {
                        return FalseV;
                    }
                } else {
                    throw new InvalidOperationException("An argument doesn't evaluate to a value");
                }
            }));
}
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
            .Extend(new Atom("lambda"), LambdaV)
            .Extend(new Atom("let"), LetV)
            .Extend(new Atom("let-many"), LetManyV)
            .Extend(new Atom("eq?"), EqV)
            .Extend(new Atom("true"), TrueV)
            .Extend(new Atom("false"), FalseV)
            .Extend(new Atom("empty?"), EmptyV)
            .Extend(new Atom("list"), ListV)
            .Extend(new Atom("+"), MathV(valueToNum, numToValue, (x, y) => x + y))
            .Extend(new Atom("-"), MathV(valueToNum, numToValue, (x, y) => x - y))
            .Extend(new Atom("*"), MathV(valueToNum, numToValue, (x, y) => x * y))
            .Extend(new Atom("/"), MathV(valueToNum, numToValue, (x, y) => x / y))
            .Extend(new Atom("pi"), EqV)
            .Extend(new Atom("e"), EqV);
    }

    // (list 1) -> (cons 1 ())
    // (list (1)) -> (cons 1 ())
    // (list (1 2 3)) -> (cons 1 (cons 2 (cons 3 ())))
    static Term<T> ListV =
        new Abs<T>((env, lst) => {
                switch (lst) {
                    case Atom: {
                        var cons = new List<Expr>();
                        cons.Add(new Atom("cons"));
                        cons.Add(lst);
                        cons.Add(new List(new List<Expr>()));
                        var expr = new List(cons);
                        return ToxicScript.EvalExpr(env, expr);
                    }
                    default: {
                        List l = (List)lst;
                        var expr = new List(new List<Expr>());
                        foreach (Expr e in l.Items) {
                            var newExpr = new List<Expr>();
                            newExpr.Add(new Atom("cons"));
                            newExpr.Add(e);
                            newExpr.Add(expr);
                            expr = new List(newExpr);
                        }
                        return ToxicScript.EvalExpr(env, expr);
                    }
                }
            });

    static Term<T> EmptyV =
        new Abs<T>((env, lst) => {
            var res = ToxicScript.EvalExpr(env, lst);
            switch (res) {
                case Exp<T> v: {
                    if ((v.Expr is List) && (((List)v.Expr).Items.Count == 0)) {
                            return TrueV!;
                        }
                    else {
                        return FalseV!;
                    }
                }
                default: return FalseV!;
            }
        });

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

    // Create the Z-combinator (strict version of the Y-combinator)
    // (lambda f (lambda x (f (lambda v (x x v))))
    //           (lambda x (f (lambda v (x x v)))))
    static Expr MkZCombE() {
        var common = "(lambda x (f (lambda v (x x v))))";
        var expr = "(lambda f (" + common + " " + common + "))";
        return Expr.Parse(expr);
    }

    static Term<T> LetManyV =
        new Abs<T>((staticEnv, defs) => {
            switch (defs) {
                case List pairs: {
                    return new Abs<T>((_, body) => {
                        var expr = body;
                        var items = pairs.Items;
                        items.Reverse();
                        foreach (Expr e in items) {
                            switch (e) {
                                case List pair: {
                                    // To make each binding recursive, embed each
                                    // name-value pair in a Z-combinator:
                                    // (let-many ((n v)) b) -> (let n (Z (lambda n v))) b)
                                    var name = pair.Items[0];
                                    var val = pair.Items[1];
                                    var newExpr = new List<Expr>();
                                    var letExpr = new List<Expr>();
                                    letExpr.Add(new Atom("let"));
                                    letExpr.Add(name);
                                    var zExpr = new List<Expr>();
                                    zExpr.Add(MkZCombE());
                                    var lambdaExpr = new List<Expr>();
                                    lambdaExpr.Add(new Atom("lambda"));
                                    lambdaExpr.Add(name);
                                    lambdaExpr.Add(val);
                                    zExpr.Add(new List(lambdaExpr));
                                    letExpr.Add(new List(zExpr));
                                    newExpr.Add(new List(letExpr));
                                    newExpr.Add(expr);

                                    expr = new List(newExpr);
                                    break;
                                }
                                default: throw new InvalidOperationException("let-many: incorrect binding");
                            }
                        }
                        return ToxicScript.EvalExpr(staticEnv, expr);
                    });
                }
                default: throw new InvalidOperationException("let-many: incorrect binding list");
            }
        });

    static Expr Stdlib() {
        var stdlibStr =
                @"(let-many
                    (
                        (first  (lambda x (lambda y x)))
                        (second (lambda x (lambda y y)))
                        (cons   (lambda x (lambda y (lambda f (f x y)))))
                        (fst    (lambda p (p first)))
                        (snd    (lambda p (p second)))
                        (head fst)
                        (tail snd)
                        (if
                            (lambda cond
                                (lambda ifTrue (lambda ifFalse (cond ifTrue ifFalse)))))
                        (map (lambda fn (lambda lst ((empty? lst) lst (cons (fn (fst lst)) (map fn (tail lst)))))))
                        (length (lambda lst ((empty? lst) 0 (+ 1 (length (tail lst))))))
                        (nth (lambda n (lambda lst (((eq? n 0) (head lst) (nth (- n 1) (tail lst)))))))
                    )
                )";
        return Expr.Parse(stdlibStr);
    }

    public static Expr WithStdlib(Expr code) {
        var newExpr = new List<Expr>();
        newExpr.Add(Stdlib());
        newExpr.Add(code);
        return new List(newExpr);
    }
}
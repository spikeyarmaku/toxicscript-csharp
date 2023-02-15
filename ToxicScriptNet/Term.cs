namespace ToxicScriptNet;

public abstract class Term<T> {
    public abstract override string ToString();

    public static Term<T> MultiAbs(
            int paramCount,
            Func<Env<Term<T>>,
            List<Expr>,
            Term<T>> body) {
        List<Expr> exprs = new List<Expr>();
        return MultiAbsHelper(exprs, paramCount, body);
    }

    private static Term<T> MultiAbsHelper(
            List<Expr> exprs,
            int paramCount,
            Func<Env<Term<T>>,
            List<Expr>,
            Term<T>> body) {
        Term<T> term;
        if (paramCount == 1) {
            term = new Abs<T>((env, p) => {
                exprs.Add(p);
                return body(env, exprs);
            });
        } else {
            term = new Abs<T>((_, p) => {
                exprs.Add(p);
                return MultiAbsHelper(exprs, paramCount - 1, body);
            });
        }
        return term;
    }
}

public class Abs<T> : Term<T> {
    public Func<Env<Term<T>>, Expr, Term<T>> ApplyAbs { get; }

    public Abs(Func<Env<Term<T>>, Expr, Term<T>> abs) {
        ApplyAbs = abs;
    }
    
    public override string ToString() {
        return "<<Abstraction>>";
    }
}

public class Exp<T> : Term<T> {
    public Expr Expr { get; }

    public Exp(Expr expr) {
        Expr = expr;
    }

    public override string ToString() {
        return "<<Variable:" + Expr.ToString() + ">>";
    }
}

public class Val<T> : Term <T> {
    public T Data { get; }

    public Val(T t) {
        Data = t;
    }

    public override string ToString() {
        return "<<Value: " + typeof(T).Name + ">>";
    }
}
namespace ToxicScriptNet;

public abstract class Term<T> {
    public abstract override string ToString();
}

public class Abs<T> : Term<T> {
    public Func<Env<Term<T>>, Expr, Term<T>> ApplyAbs { get; }

    public Abs(Func<Env<Term<T>>, Expr, Term<T>> abs) {
        ApplyAbs = abs;
    }
    
    public override string ToString() {
        return "<<Transform>>";
    }
}

public class Var<T> : Term<T> {
    public Expr Expr { get; }

    public Var(Expr expr) {
        Expr = expr;
    }

    public override string ToString() {
        return "<<Promise:" + Expr.ToString() + ">>";
    }
}

public class Val<T> : Term <T> {
    public T Data { get; }

    public Val(T t) {
        Data = t;
    }

    public override string ToString() {
        return "<<Opaque value: " + typeof(T).Name + ">>";
    }
}
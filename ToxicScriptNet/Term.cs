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
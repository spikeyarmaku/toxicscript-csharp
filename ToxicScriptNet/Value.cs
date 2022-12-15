namespace ToxicScriptNet;

public abstract class Value<T> {
    public abstract override string ToString();
}

public class Transform<T> : Value<T> {
    public Func<Env<Value<T>>, List<Expr>, Value<T>> ApplyTransform { get; }

    public Transform(Func<Env<Value<T>>, List<Expr>, Value<T>> transform) {
        ApplyTransform = transform;
    }
    
    public override string ToString() {
        return "<<Transform>>";
    }
}

public class Promise<T> : Value<T> {
    public Env<Value<T>> Env { get; }
    public Expr Expr { get; }

    public Promise(Env<Value<T>> env, Expr expr) {
        Env = env;
        Expr = expr;
    }

    public override string ToString() {
        return "<<Promise:" + Expr.ToString() + ">>";
    }
}

public class Opaque<T> : Value <T> {
    public T Data { get; }

    public Opaque(T t) {
        Data = t;
    }

    public override string ToString() {
        return "<<Opaque value: " + typeof(T).Name + ">>";
    }
}
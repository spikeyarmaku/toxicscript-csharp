using System.Numerics;

namespace ToxicScriptNet;

public class Env<T> {
    Func<Expr, T?> Mapping = (expr) => default(T);
    Env<T>? Parent;

    protected Env() { }

    public void SetMapping(Func<Expr, T?> mapping) {
        Mapping = mapping;
    }

    public T? Lookup(Expr expr) {
        return Mapping(expr);
    }

    public static Env<T> Empty() {
        return new Env<T>();
    }

    public static Env<T> Default(Func<float, T> numToValue,
        Func<string, T> stringToValue)
    {
        var newEnv = new Env<T>();
        newEnv.Mapping = (e) => {
            if (e is Symbol) {
                var s = (Symbol)e;
                float result;
                if (float.TryParse(s.Name, out result)) {
                    return numToValue(result);
                } else if (s.Name.StartsWith('"') && s.Name.EndsWith('"')) {
                    return stringToValue(s.Name);
                } else {
                    return default(T);
                }
            } else {
                return default(T);
            }
        };
        return newEnv;
    }

    public Env<T> Extend(Expr expr, T t) {
        var newEnv = new Env<T>();
        newEnv.Parent = this;
        newEnv.Mapping = (e) => {
            if (e.Equals(expr)) {
                return t;
            } else {
                return Lookup(e);
            }
        };
        return newEnv;
    }

    public Env<T> Extend(string name, T t) {
        var nameExpr = new Symbol(name);
        return Extend(nameExpr, t);
    }

    public Env<T> Extend(string name, Func<Expr, T> namedT) {
        var nameExpr = new Symbol(name);
        var t = namedT(nameExpr);

        return Extend(nameExpr, t);
    }
}
// https://learn.microsoft.com/en-us/dotnet/core/tutorials/library-with-visual-studio-code?pivots=dotnet-7-0

namespace ToxicScriptNet;

static public class ToxicScript
{
    public static Expr Parse(string input) {
        return Expr.Parse(input);
    }

    public static Term<T> Eval<T>(Env<Term<T>> env, Term<T> term) {
        switch (term) {
            case Val<T> v: return v;
            case Var<T> v:
                var val = env.Lookup(v.Expr);
                return val == null ? v : val;
            case Abs<T> f: return f;
            default:
                throw new InvalidOperationException("PANIC! Eval encountered a term of invalid type");
        }
    }

    public static Term<T> Apply<T>(Env<Term<T>> env, Term<T> t, Expr e) {
        switch (t) {
            case Abs<T> f: return (f.ApplyAbs(env, e));
            default:
                var v = Eval(env, t);
                return Apply(env, v, e);
        }
    }

    public static Term<T> EvalExpr<T>(Env<Term<T>> env, Expr e) {
        var v = env.Lookup(e);
        if (v == null) {
            switch(e) {
                case Symbol s: throw new InvalidOperationException("Unassigned variable: " + s.ToString());
                default:
                    List l = (List)e;
                    var c = l.GetInit();
                    var p = l.GetLast();
                    if (c is List && ((List)c).IsValidCombination()) {
                        var val = EvalExpr(env, c!);
                        return Apply(env, val, p!);
                    } else {
                        return EvalExpr(env, p!);
                    }
            }
        } else {
            return v;
        }
    }
}

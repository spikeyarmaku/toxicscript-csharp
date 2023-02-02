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
            case Exp<T> v:
                var val = env.Lookup(v.Expr);
                return val == null ? v : val;
            case Abs<T> f: return f;
            default:
                throw new InvalidOperationException("PANIC! Eval encountered a term of invalid type");
        }
    }

    public static Term<T> Apply<T>(Env<Term<T>> env, Term<T> t, Expr e) {
        // Console.WriteLine("--- Applying " + t.ToString() + " to " + e.ToString());
        switch (t) {
            case Val<T>: throw new InvalidOperationException("Cannot use value as a function");
            case Abs<T> f:
                return (f.ApplyAbs(env, e));
            default:
                var v = Eval(env, t);
                Exp<T> var = (Exp<T>)t;
                if (v is Exp<T> var2 && var.Expr == var2.Expr) {
                    throw new InvalidOperationException("Cannot evaluate " + v.ToString());
                } else {
                    return Apply(env, v, e);
                }
        }
    }

    public static Term<T> EvalExpr<T>(Env<Term<T>> env, Expr e) {
        // Console.WriteLine("[INFO] Evaluating: " + e.ToString());
        var v = env.Lookup(e);
        if (v == null) {
            // Console.WriteLine("No value assigned");
            switch(e) {
                case Atom s: throw new InvalidOperationException("Unassigned variable: " + s.ToString());
                default:
                    List l = (List)e;
                    if (l.Items.Count == 0) {
                        // On an empty list, return the empty list as a variable
                        return new Exp<T>(l);
                    } else {
                        // Console.WriteLine("Valid combination");
                        // The list contains at least one item
                        var c = l.GetInit();
                        var p = l.GetLast();
                        if (c is List cl && cl.Items.Count != 0) {
                            // Console.WriteLine("Compound expression");
                            var val = EvalExpr(env, c!);
                            return Apply(env, val, p!);
                        } else {
                            // Console.WriteLine("Single expression");
                            return EvalExpr(env, p!);
                        }
                    }
            }
        } else {
            return v;
        }
    }
}

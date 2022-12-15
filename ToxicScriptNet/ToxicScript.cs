// https://learn.microsoft.com/en-us/dotnet/core/tutorials/library-with-visual-studio-code?pivots=dotnet-7-0

namespace ToxicScriptNet;

static public class ToxicScript
{
    public static Expr Parse(string input) {
        return Expr.Parse(input);
    }

    public static Value<T> Eval<T>(Env<Value<T>> env, Expr expr) {
        // Check if the expression has a value assigned to it
        var val = env.Lookup(expr);
        if (val == null) {
            // If not, check if it is a valid combination
            if (expr is List && ((List)expr).IsValidCombination()) {
                // If it is, evaluate the combiner, and apply the parameters
                List l = (List)expr;
                return EvalCombination(env, l.GetHead()!, l.GetTail());
            } else {
                // No value assigned, and not a valid combination
                throw new InvalidOperationException("No value assigned to " + expr.ToString());
            }
        } else {
            // Evaluate the assigned value if necessary, and return it
            if (val is Promise<T>) {
                Promise<T> p = (Promise<T>)val;
                return Eval(p.Env, p.Expr);
            } else {
                return val;
            }
        }
    }

    private static Value<T> EvalCombination<T>(Env<Value<T>> env, Expr combiner,
        List<Expr> prms)
    {
        // Evaluate the combiner
        var comb = Eval(env, combiner);
        if (comb == null) {
            throw new InvalidOperationException("Cannot evaluate combiner");
        } else {
            switch (comb) {
                case Promise<T> p:
                    return EvalCombination(p.Env, p.Expr, prms);
                case Opaque<T> o:
                    if (prms.Count == 0) {
                        return o;
                    } else {
                        throw new InvalidOperationException("Too many arguments");
                    }
                case Transform<T> tr:
                    return tr.ApplyTransform(env, prms);
                default:
                    throw new InvalidOperationException("Cannot evaluate combiner");
            }
        }
    }
}

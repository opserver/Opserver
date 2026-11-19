using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jil.Common;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace Jil.DeserializeDynamic
{
    sealed partial class JsonObject : IDynamicMetaObjectProvider
    {
        public DynamicMetaObject GetMetaObject(Expression exp)
        {
            return new JsonMetaObject(this, exp);
        }

        sealed class JsonMetaObject : DynamicMetaObject
        {
            public JsonObject Outer { get { return (JsonObject)Value; } }

            public JsonMetaObject(JsonObject outer, Expression exp) : base(exp, BindingRestrictions.Empty, outer) { }

            static ConstructorInfo InvalidCastExceptionCons = typeof(InvalidCastException).GetConstructor(new[] { typeof(string) });
            static MethodInfo StringConcat = typeof(string).GetMethod("Concat", new[] { typeof(object), typeof(object), typeof(object) });
            static MethodInfo StringConcatArray = typeof(string).GetMethod("Concat", new[] { typeof(object[]) });
            static MethodInfo StringJoin = typeof(string).GetMethod("Join", new[] { typeof(string), typeof(object[]) });

            static ParameterExpression ThisEvaled = Expression.Variable(typeof(JsonObject));
            static ParameterExpression Res = Expression.Variable(typeof(object));
            static ParameterExpression ArgRef = Expression.Variable(typeof(object));
            static ConstantExpression UnableToConvertDynamic = Expression.Constant("Unable to convert dynamic [");
            static ConstantExpression CommaSpace = Expression.Constant(", ");
            static ConstantExpression CloseSquareBracket = Expression.Constant("]");
            static ParameterExpression IndexesRef = Expression.Variable(typeof(object[]));
            static ConstantExpression UnableToGetDynamicIndex = Expression.Constant("Unable to get dynamic index (");
            static ConstantExpression CloseBraceOpenSquareBracket = Expression.Constant(") on [");

            public override DynamicMetaObject BindConvert(ConvertBinder binder)
            {
                /*
                 * Effectively, this returns an expression of the following code:
                 * {
                 *      JsonObject thisEvaled = (JsonObject)<Expression>;
                 *      ReturnType finalResult;
                 *      object res;
                 *      if(!Value.InnerTryConvert(ReturnType, out res))
                 *      {
                 *          throw new InvalidCastException("Unable to convert dynamic ["+thisEvaled+"] to "+ReturnType.FullName");
                 *      }
                 *      finalResult = (ReturnType)res;
                 * }
                 * 
                 * Unlike C#, block expression in LINQ do produce a value when evaluated.  It's the last expression executed.
                 */

                var thisRef = Expression.Type != typeof(JsonObject) ? Expression.Convert(Expression, typeof(JsonObject)) : Expression;
                var thisEvaled = ThisEvaled;
                var thisAssigned = Expression.Assign(thisEvaled, thisRef);
                var finalResult = Expression.Variable(binder.ReturnType);
                var res = Res;
                var tryConvertCall = Expression.Call(thisEvaled, InnerTryConvertMtd, Expression.Constant(binder.ReturnType), res);
                var throwExc =
                    Expression.Throw(
                        Expression.New(
                            InvalidCastExceptionCons,
                            Expression.Call(
                                StringConcat,
                                UnableToConvertDynamic,
                                Expression.Constant(Outer.Type.ToString()),
                                Expression.Constant("] to [" + binder.ReturnType.FullName + "]")
                            )
                        )
                    );

                var notIf = 
                    Expression.IfThen(
                        Expression.Not(tryConvertCall),
                        throwExc
                    );
                var finalAssign = Expression.Assign(finalResult, Expression.Convert(res, binder.ReturnType));
                
                var retBlock = Expression.Block(new[] { thisEvaled, finalResult, res }, thisAssigned, notIf, finalAssign);
                var restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                return new DynamicMetaObject(retBlock, restrictions);
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                /* 
                 * Effectively returns the following code:
                 * {
                 *      var thisEvaled = (JsonObject)<Expression>;
                 *      object res;
                 *      ReturnType finalResult;
                 *      if(!Value.InnerTryGetMember(<MemberName>, ReturnType, out res))
                 *      {
                 *          throw new InvalidCastException("Unable to get dynamic member <MemberName> of type <ReturnType> from ["+thisRef+"]");
                 *      }
                 *      finalResult = (ReturnType)res;
                 * }
                 */

                var thisRef = Expression.Type != typeof(JsonObject) ? Expression.Convert(Expression, typeof(JsonObject)) : Expression;
                var thisEvaled = ThisEvaled;
                var thisAssigned = Expression.Assign(thisEvaled, thisRef);
                var finalResult = Expression.Variable(binder.ReturnType);
                var res = Res;
                var tryGetMemberCall = Expression.Call(thisEvaled, InnerTryGetMember, Expression.Constant(binder.Name), Expression.Constant(binder.ReturnType), res);
                var throwExc =
                    Expression.Throw(
                        Expression.New(
                            InvalidCastExceptionCons,
                            Expression.Call(
                                StringConcat,
                                Expression.Constant("Unable to get dynamic member [" + binder.Name + "] of type [" + binder.ReturnType.FullName + "] from ["),
                                thisEvaled,
                                CloseSquareBracket
                            )
                        )
                    );

                var notIf =
                    Expression.IfThen(
                        Expression.Not(tryGetMemberCall),
                        throwExc
                    );
                var finalAssign = Expression.Assign(finalResult, Expression.Convert(res, binder.ReturnType));

                var retBlock = Expression.Block(new[] { thisEvaled, finalResult, res }, thisAssigned, notIf, finalAssign);
                var restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                return new DynamicMetaObject(retBlock, restrictions);
            }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                /* Effectively returns the following code:
                 * {
                 *      var thisEvaled = (JsonObject)<Expression>;
                 *      object res;
                 *      ReturnType finalResult;
                 *      object[] indexesRef = new [] { <index 0>, <index 1>, ... };
                 *      if(!Value.InnerTryGetIndex(ReturnType, indexesRef, out res))
                 *      {
                 *          throw new InvalidCastException("Unable to get dynamic index ("+string.Join(", ", indexesRef)+") of type <ReturnType> from ["+thisRef+"]");
                 *      }
                 *      finalResult = (ReturnType)res;
                 * }
                 */

                var indexExprs = new Expression[indexes.Length];
                for (var i = 0; i < indexes.Length; i++)
                {
                    var index = indexes[i];
                    var exp = index.Expression;

                    if (exp.Type.IsValueType())
                    {
                        indexExprs[i] = Expression.Convert(exp, typeof(object));
                    }
                    else
                    {
                        indexExprs[i] = exp;
                    }
                }

                var thisRef = Expression.Type != typeof(JsonObject) ? Expression.Convert(Expression, typeof(JsonObject)) : Expression;
                var thisEvaled = ThisEvaled;
                var thisAssigned = Expression.Assign(thisEvaled, thisRef);
                var finalResult = Expression.Variable(binder.ReturnType);
                var res = Res;
                var indexesRef = IndexesRef;
                var indexesAssigned = Expression.Assign(indexesRef, Expression.NewArrayInit(typeof(object), indexExprs));
                var tryGetIndexCall = Expression.Call(thisEvaled, InnerTryGetIndex, Expression.Constant(binder.ReturnType), indexesRef, res);
                var throwExc =
                    Expression.Throw(
                        Expression.New(
                            InvalidCastExceptionCons,
                            Expression.Call(
                                StringConcatArray,
                                Expression.NewArrayInit(
                                    typeof(object),
                                    UnableToGetDynamicIndex,
                                    Expression.Call(StringJoin, CommaSpace, indexesRef),
                                    Expression.Constant("of type ["+binder.ReturnType.FullName+"] from ["),
                                    thisRef,
                                    CloseSquareBracket
                                )
                            )
                        )
                    );

                var notIf =
                    Expression.IfThen(
                        Expression.Not(tryGetIndexCall),
                        throwExc
                    );
                var finalAssign = Expression.Assign(finalResult, Expression.Convert(res, binder.ReturnType));

                var retBlock = Expression.Block(new[] { thisEvaled, finalResult, res, indexesRef }, thisAssigned, indexesAssigned, notIf, finalAssign);
                var restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                return new DynamicMetaObject(retBlock, restrictions);
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                /* Effectively returns the following code:
                 * {
                 *      var thisEvaled = (JsonObject)<Expression>;
                 *      object res;
                 *      ReturnType finalResult;
                 *      object[] argsRef = new [] { <args 0>, <args 1>, ... };
                 *      if(!Value.InnerTryInvokeMember(<MemberName>, argsRef, out res))
                 *      {
                 *          throw new InvalidCastException("Unable to invoke dynamic member <MemberName> with args ("+string.Join(", ", argsRef)+") on ["+thisRef+"]");
                 *      }
                 *      finalResult = (ReturnType)res;
                 * }
                 */

                var argExprs = new Expression[args.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    var exp = arg.Expression;

                    if (exp.Type.IsValueType())
                    {
                        argExprs[i] = Expression.Convert(exp, typeof(object));
                    }
                    else
                    {
                        argExprs[i] = exp;
                    }
                }

                var thisRef = Expression.Type != typeof(JsonObject) ? Expression.Convert(Expression, typeof(JsonObject)) : Expression;
                var thisEvaled = ThisEvaled;
                var thisAssigned = Expression.Assign(thisEvaled, thisRef);
                var finalResult = Expression.Variable(binder.ReturnType);
                var argsRef = IndexesRef;
                var argsAssigned = Expression.Assign(argsRef, Expression.NewArrayInit(typeof(object), argExprs));
                var res = Res;
                var tryInvokeMemberCall = Expression.Call(thisEvaled, InnerTryInvokeMember, Expression.Constant(binder.Name), argsRef, res);
                var throwExc =
                    Expression.Throw(
                        Expression.New(
                            InvalidCastExceptionCons,
                            Expression.Call(
                                StringConcatArray,
                                Expression.NewArrayInit(
                                    typeof(object),
                                    Expression.Constant("Unable to invoke dynamic member [" + binder.Name + "] with args ("),
                                    Expression.Call(StringJoin, CommaSpace, argsRef),
                                    CloseBraceOpenSquareBracket,
                                    thisEvaled,
                                    CloseSquareBracket
                                )
                            )
                        )
                    );

                var notIf =
                    Expression.IfThen(
                        Expression.Not(tryInvokeMemberCall),
                        throwExc
                    );
                var finalAssign = Expression.Assign(finalResult, Expression.Convert(res, binder.ReturnType));

                var retBlock = Expression.Block(new[] { thisEvaled, finalResult, res, argsRef }, thisAssigned, argsAssigned, notIf, finalAssign);
                var restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                return new DynamicMetaObject(retBlock, restrictions);
            }

            public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
            {
                /* 
                 * Effectively returns the following code:
                 * {
                 *      var thisEvaled = (JsonObject)<Expression>;
                 *      object res;
                 *      ReturnType finalResult;
                 *      if(!Value.InnerTryUnaryOperation(ExpressionType, ReturnType, out res))
                 *      {
                 *          throw new InvalidCastException("Unable to get dynamic member <MemberName> of type <ReturnType> from ["+thisRef+"]");
                 *      }
                 *      finalResult = (ReturnType)res;
                 * }
                 */

                var thisRef = Expression.Type != typeof(JsonObject) ? Expression.Convert(Expression, typeof(JsonObject)) : Expression;
                var thisEvaled = ThisEvaled;
                var thisAssigned = Expression.Assign(thisEvaled, thisRef);
                var finalResult = Expression.Variable(binder.ReturnType);
                var res = Res;
                var tryUnaryOperationCall = Expression.Call(thisEvaled, InnerTryUnaryOperation, Expression.Constant(binder.Operation), Expression.Constant(binder.ReturnType), res);
                var throwExc =
                    Expression.Throw(
                        Expression.New(
                            InvalidCastExceptionCons,
                            Expression.Call(
                                StringConcat,
                                Expression.Constant("Unable to perform dynamic unary operation [" + binder.Operation + "] of type [" + binder.ReturnType.FullName + "] from ["),
                                thisEvaled,
                                CloseSquareBracket
                            )
                        )
                    );

                var notIf =
                    Expression.IfThen(
                        Expression.Not(tryUnaryOperationCall),
                        throwExc
                    );
                var finalAssign = Expression.Assign(finalResult, Expression.Convert(res, binder.ReturnType));

                var retBlock = Expression.Block(new[] { thisEvaled, finalResult, res }, thisAssigned, notIf, finalAssign);
                var restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                return new DynamicMetaObject(retBlock, restrictions);
            }

            public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
            {
                /* Effectively returns the following code:
                 * {
                 *      var thisEvaled = (JsonObject)<Expression>;
                 *      object res;
                 *      ReturnType finalResult;
                 *      if(!Value.InnerTryBinaryOperations(Operand, arg, ReturnType, out res))
                 *      {
                 *          throw new InvalidCastException("Unable to invoke dynamic binary operation [<Operation>] on ["+thisRef+"]");
                 *      }
                 *      finalResult = (ReturnType)res;
                 * }
                 */

                var thisRef = Expression.Type != typeof(JsonObject) ? Expression.Convert(Expression, typeof(JsonObject)) : Expression;
                var thisEvaled = ThisEvaled;
                var thisAssigned = Expression.Assign(thisEvaled, thisRef);
                var finalResult = Expression.Variable(binder.ReturnType);
                var argRef = ArgRef;
                var argAssigned = Expression.Assign(argRef, (arg.RuntimeType == null || arg.RuntimeType.IsValueType()) ? Expression.Convert(arg.Expression, typeof(object)) : arg.Expression);
                var res = Res;
                var tryInvokeMemberCall = Expression.Call(thisEvaled, InnerTryBinaryOperation, Expression.Constant(binder.Operation), argRef, Expression.Constant(binder.ReturnType), res);
                var throwExc =
                    Expression.Throw(
                        Expression.New(
                            InvalidCastExceptionCons,
                            Expression.Call(
                                StringConcat,
                                Expression.Constant("Unable to invoke dynamic binary operation [" + binder.Operation + "] on ["),
                                thisEvaled,
                                CloseSquareBracket
                            )
                        )
                    );

                var notIf =
                    Expression.IfThen(
                        Expression.Not(tryInvokeMemberCall),
                        throwExc
                    );
                var finalAssign = Expression.Assign(finalResult, Expression.Convert(res, binder.ReturnType));

                var retBlock = Expression.Block(new[] { thisEvaled, finalResult, res, argRef }, thisAssigned, argAssigned, notIf, finalAssign);
                var restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                return new DynamicMetaObject(retBlock, restrictions);
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return Outer.GetDynamicMemberNames();
            }
        }

        static MethodInfo InnerTryBinaryOperation = typeof(JsonObject).GetMethod("_InnerTryBinaryOperation", BindingFlags.Instance | BindingFlags.NonPublic);
        bool _InnerTryBinaryOperation(ExpressionType operand, dynamic rightHand, Type returnType, out object result)
        {
            switch(operand)
            {
                case ExpressionType.And:
                    if (!returnType.IsAssignableFrom(typeof(bool)))
                    {
                        result = null;
                        return false;
                    }
                    if (Type == JsonObjectType.False)
                    {
                        result = false;
                        return true;
                    }
                    if (Type == JsonObjectType.True)
                    {
                        result = (bool)rightHand;
                        return true;
                    }
                    break;

                case ExpressionType.Or:
                    if (!returnType.IsAssignableFrom(typeof(bool)))
                    {
                        result = null;
                        return false;
                    }
                    if (Type == JsonObjectType.False)
                    {
                        result = (bool)rightHand;
                        return true;
                    }
                    if (Type == JsonObjectType.True)
                    {
                        result = true;
                        return true;
                    }
                    break;

                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    if (!returnType.IsAssignableFrom(typeof(float)))
                    {
                        result = null;
                        return false;
                    }
                    if(Type == JsonObjectType.Number || Type == JsonObjectType.FastNumber)
                    {
                        object lhsRef;
                        if (InnerTryConvert(typeof(float), out lhsRef))
                        {
                            var lhs = (float)lhsRef;
                            var rhs = (float)rightHand;

                            result = lhs + rhs;
                            return true;
                        }
                    }
                    break;

                case ExpressionType.Divide:
                    if (!returnType.IsAssignableFrom(typeof(float)))
                    {
                        result = null;
                        return false;
                    }
                    if(Type == JsonObjectType.Number || Type == JsonObjectType.FastNumber)
                    {
                        object lhsRef;
                        if (InnerTryConvert(typeof(float), out lhsRef))
                        {
                            var lhs = (float)lhsRef;
                            var rhs = (float)rightHand;

                            result = lhs / rhs;
                            return true;
                        }
                    }
                    break;

                case ExpressionType.Equal:
                    if (!returnType.IsAssignableFrom(typeof(bool)))
                    {
                        result = null;
                        return false;
                    }
                    if (object.ReferenceEquals(rightHand, null))
                    {
                        result = false;
                        return true;
                    }
                    if (object.ReferenceEquals(this, rightHand))
                    {
                        result = true;
                        return true;
                    }
                    if (Type == JsonObjectType.FastNumber || Type == JsonObjectType.Number)
                    {
                        object leftHand;
                        if (InnerTryConvert(typeof(float), out leftHand))
                        {
                            var rhs = (float)rightHand;
                            var lhs = (float)leftHand;

                            result = lhs == rhs;
                            return true;
                        }
                    }
                    if (Type == JsonObjectType.String)
                    {
                        object leftHand;
                        if (InnerTryConvert(typeof(string), out leftHand))
                        {
                            var rhs = (string)rightHand;
                            var lhs = (string)leftHand;

                            result = lhs == rhs;
                            return true;
                        }
                    }
                    if (Type == JsonObjectType.True)
                    {
                        result = (bool)rightHand;
                        return true;
                    }
                    if (Type == JsonObjectType.False)
                    {
                        result = !(bool)rightHand;
                        return true;
                    }
                    break;

                case ExpressionType.GreaterThan:
                    if (!returnType.IsAssignableFrom(typeof(bool)))
                    {
                        result = null;
                        return false;
                    }
                    if (Type == JsonObjectType.Number || Type == JsonObjectType.FastNumber)
                    {
                        object lhsRef;
                        if (InnerTryConvert(typeof(float), out lhsRef))
                        {
                            var lhs = (float)lhsRef;
                            var rhs = (float)rightHand;

                            result = lhs > rhs;
                            return true;
                        }
                    }
                    break;

                case ExpressionType.GreaterThanOrEqual:
                    if (!returnType.IsAssignableFrom(typeof(bool)))
                    {
                        result = null;
                        return false;
                    }
                    if (Type == JsonObjectType.Number || Type == JsonObjectType.FastNumber)
                    {
                        object lhsRef;
                        if (InnerTryConvert(typeof(float), out lhsRef))
                        {
                            var lhs = (float)lhsRef;
                            var rhs = (float)rightHand;

                            result = lhs >= rhs;
                            return true;
                        }
                    }
                    break;

                case ExpressionType.LessThan: 
                    if (!returnType.IsAssignableFrom(typeof(bool)))
                    {
                        result = null;
                        return false;
                    }
                    if (Type == JsonObjectType.Number || Type == JsonObjectType.FastNumber)
                    {
                        object lhsRef;
                        if (InnerTryConvert(typeof(float), out lhsRef))
                        {
                            var lhs = (float)lhsRef;
                            var rhs = (float)rightHand;

                            result = lhs < rhs;
                            return true;
                        }
                    }
                    break;

                case ExpressionType.LessThanOrEqual: 
                    if (!returnType.IsAssignableFrom(typeof(bool)))
                    {
                        result = null;
                        return false;
                    }
                    if (Type == JsonObjectType.Number || Type == JsonObjectType.FastNumber)
                    {
                        object lhsRef;
                        if (InnerTryConvert(typeof(float), out lhsRef))
                        {
                            var lhs = (float)lhsRef;
                            var rhs = (float)rightHand;

                            result = lhs <= rhs;
                            return true;
                        }
                    }
                    break;

                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    if (!returnType.IsAssignableFrom(typeof(float)))
                    {
                        result = null;
                        return false;
                    }
                    if(Type == JsonObjectType.Number || Type == JsonObjectType.FastNumber)
                    {
                        object lhsRef;
                        if (InnerTryConvert(typeof(float), out lhsRef))
                        {
                            var lhs = (float)lhsRef;
                            var rhs = (float)rightHand;

                            result = lhs * rhs;
                            return true;
                        }
                    }
                    break;

                case ExpressionType.NotEqual:
                    if (!returnType.IsAssignableFrom(typeof(bool)))
                    {
                        result = null;
                        return false;
                    }
                    if (object.ReferenceEquals(rightHand, null))
                    {
                        result = true;
                        return true;
                    }
                    if (object.ReferenceEquals(this, rightHand))
                    {
                        result = false;
                        return true;
                    }
                    if (Type == JsonObjectType.FastNumber || Type == JsonObjectType.Number)
                    {
                        object leftHand;
                        if (InnerTryConvert(typeof(float), out leftHand))
                        {
                            var rhs = (float)rightHand;
                            var lhs = (float)leftHand;

                            result = lhs != rhs;
                            return true;
                        }
                    }
                    if (Type == JsonObjectType.String)
                    {
                        object leftHand;
                        if (InnerTryConvert(typeof(string), out leftHand))
                        {
                            var rhs = (string)rightHand;
                            var lhs = (string)leftHand;

                            result = lhs != rhs;
                            return true;
                        }
                    }
                    if (Type == JsonObjectType.True)
                    {
                        result = !(bool)rightHand;
                        return true;
                    }
                    if (Type == JsonObjectType.False)
                    {
                        result = (bool)rightHand;
                        return true;
                    }
                    break;

                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    if (!returnType.IsAssignableFrom(typeof(float)))
                    {
                        result = null;
                        return false;
                    }
                    if(Type == JsonObjectType.Number || Type == JsonObjectType.FastNumber)
                    {
                        object lhsRef;
                        if (InnerTryConvert(typeof(float), out lhsRef))
                        {
                            var lhs = (float)lhsRef;
                            var rhs = (float)rightHand;

                            result = lhs - rhs;
                            return true;
                        }
                    }
                    break;
            }

            result = null;
            return false;
        }

        static MethodInfo InnerTryUnaryOperation = typeof(JsonObject).GetMethod("_InnerTryUnaryOperation", BindingFlags.Instance | BindingFlags.NonPublic);
        bool _InnerTryUnaryOperation(ExpressionType operand, Type returnType, out object result)
        {
            switch(operand)
            {
                case ExpressionType.IsFalse:
                    if(!returnType.IsAssignableFrom(typeof(bool)))
                    {
                        result = null;
                        return false;
                    }
                    result = Type == JsonObjectType.False;
                    return true;
                
                case ExpressionType.IsTrue:
                    if(!returnType.IsAssignableFrom(typeof(bool)))
                    {
                        result = null;
                        return false;
                    }
                    result = Type == JsonObjectType.True;
                    return true;

                case ExpressionType.UnaryPlus:
                    if (Type == JsonObjectType.FastNumber || Type == JsonObjectType.Number)
                    {
                        return this.InnerTryConvert(returnType, out result);
                    }
                    break;

                case ExpressionType.NegateChecked:
                case ExpressionType.Negate:
                    if (Type == JsonObjectType.FastNumber)
                    {
                        var negated = 
                            new JsonObject
                            {
                                Type = JsonObjectType.FastNumber,

                                FastNumberNegative = !this.FastNumberNegative,
                                FastNumberPart1 = this.FastNumberPart1,
                                FastNumberPart2 = this.FastNumberPart2,
                                FastNumberPart2Length = this.FastNumberPart2Length,
                                FastNumberPart3 = this.FastNumberPart3
                            };

                        return negated.InnerTryConvert(returnType, out result);
                    }
                    if (Type == JsonObjectType.Number)
                    {
                        var negated =
                            new JsonObject
                            {
                                Type = JsonObjectType.Number,

                                NumberValue = -this.NumberValue
                            };

                        return negated.InnerTryConvert(returnType, out result);
                    }
                    break;

                case ExpressionType.Not:
                    if (Type == JsonObjectType.True)
                    {
                        return JsonObject.False.InnerTryConvert(returnType, out result);
                    }
                    if (Type == JsonObjectType.False)
                    {
                        return JsonObject.True.InnerTryConvert(returnType, out result);
                    }
                    break;
            }

            result = null;
            return false;
        }

        static MethodInfo InnerTryGetIndex = typeof(JsonObject).GetMethod("_InnerTryGetIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        bool _InnerTryGetIndex(Type returnType, object[] indexes, out object result)
        {
            if (Type == JsonObjectType.Array)
            {
                if (indexes.Length != 1)
                {
                    result = null;
                    return false;
                }

                var indexRaw = indexes[0];
                if (!(indexRaw is int))
                {
                    result = null;
                    return false;
                }

                var ix = (int)indexRaw;
                if (ix < 0 || ix >= ArrayValue.Count)
                {
                    result = null;
                    return false;
                }

                var val = ArrayValue[ix];
                return val.InnerTryConvert(returnType, out result);
            }

            if (Type == JsonObjectType.Object)
            {
                if (indexes.Length != 1)
                {
                    result = null;
                    return false;
                }

                var key = indexes[0] as string;
                if (key == null)
                {
                    result = null;
                    return false;
                }

                JsonObject rawValue;
                if (!ObjectMembers.TryGetValue(key, out rawValue))
                {
                    result = null;
                    return false;
                }

                if (rawValue == null) {
                    result = null;
                    return true;
                }

                return rawValue.InnerTryConvert(returnType, out result);
            }

            result = null;
            return false;
        }

        static readonly IEnumerable<string> ArrayMembers = new[] { "Length", "Count" };
        IEnumerable<string> GetDynamicMemberNames()
        {
            if (Type == JsonObjectType.Object)
            {
                return ObjectMembers.Keys;
            }

            if (Type == JsonObjectType.Array)
            {
                return ArrayMembers;
            }

            return Enumerable.Empty<string>();
        }

        static MethodInfo InnerTryGetMember = typeof(JsonObject).GetMethod("_InnerTryGetMember", BindingFlags.NonPublic | BindingFlags.Instance);
        bool _InnerTryGetMember(string name, Type returnType, out object result)
        {
            if (Type == JsonObjectType.Array)
            {
                if (name == "Count" || name == "Length")
                {
                    result = ArrayValue.Count;
                    return true;
                }

                result = null;
                return false;
            }

            if (Type != JsonObjectType.Object)
            {
                result = null;
                return false;
            }
            
            JsonObject val;
            if (!ObjectMembers.TryGetValue(name, out val))
            {
                // for fetching a MEMBER, return null if we don't have it
                // that was `var foo = obj.misssing_prop == null;` can work
                result = null;
                return true;
            }

            if (val == null)
            {
                result = null;
                return true;
            }

            var ret = val.InnerTryConvert(returnType, out result);

            return ret;
        }

        static MethodInfo InnerTryConvertMtd = typeof(JsonObject).GetMethod("InnerTryConvert", BindingFlags.NonPublic | BindingFlags.Instance);
        bool InnerTryConvert(Type returnType, out object result)
        {
            if (returnType == typeof(object))
            {
                result = this;
                return true;
            }

            if (returnType.IsNullableType())
            {
                returnType = Nullable.GetUnderlyingType(returnType);
            }

            switch (Type)
            {
                case JsonObjectType.False:
                    result = false;
                    return returnType == typeof(bool);
                case JsonObjectType.True:
                    result = true;
                    return returnType == typeof(bool);
                case JsonObjectType.FastNumber:
                    if (returnType == typeof(double))
                    {
                        double res;
                        var ret = FastNumberToDouble(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(float))
                    {
                        float res;
                        var ret = FastNumberToFloat(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(decimal))
                    {
                        decimal res;
                        var ret = FastNumberToDecimal(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(byte))
                    {
                        byte res;
                        var ret = FastNumberToByte(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(sbyte))
                    {
                        sbyte res;
                        var ret = FastNumberToSByte(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(short))
                    {
                        short res;
                        var ret = FastNumberToShort(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(ushort))
                    {
                        ushort res;
                        var ret = FastNumberToUShort(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(int))
                    {
                        int res;
                        var ret = FastNumberToInt(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(uint))
                    {
                        uint res;
                        var ret = FastNumberToUInt(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(long))
                    {
                        long res;
                        var ret = FastNumberToLong(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(ulong))
                    {
                        ulong res;
                        var ret = FastNumberToULong(out res);
                        result = res;
                        return ret;
                    }
                    if (returnType == typeof(DateTime))
                    {
                        long res;
                        var ret = FastNumberToLong(out res);
                        if (!ret)
                        {
                            result = null;
                            return false;
                        }
                        switch(Options.UseDateTimeFormat)
                        {
                            case DateTimeFormat.MillisecondsSinceUnixEpoch:
                                result = Methods.UnixEpoch + TimeSpan.FromMilliseconds(res);
                                return true;
                            case DateTimeFormat.SecondsSinceUnixEpoch:
                                result = Methods.UnixEpoch + TimeSpan.FromSeconds(res);
                                return true;
                            default:
                                result = null;
                                return false;
                        }
                    }
                    if(returnType == typeof(DateTimeOffset))
                    {
                        long res;
                        var ret = FastNumberToLong(out res);
                        if (!ret)
                        {
                            result = null;
                            return false;
                        }
                        switch (Options.UseDateTimeFormat)
                        {
                            case DateTimeFormat.MillisecondsSinceUnixEpoch:
                                result = Methods.UnixEpochOffset + TimeSpan.FromMilliseconds(res);
                                return true;
                            case DateTimeFormat.SecondsSinceUnixEpoch:
                                result = Methods.UnixEpochOffset + TimeSpan.FromSeconds(res);
                                return true;
                            default:
                                result = null;
                                return false;
                        }
                    }
                    if(returnType == typeof(TimeSpan))
                    {
                        const double TicksPerMillisecond = 10000;
                        const double TicksPerSecond = 10000000;

                        double res;
                        var ret = FastNumberToDouble(out res);
                        if (!ret)
                        {
                            result = null;
                            return false;
                        }
                        switch (Options.UseDateTimeFormat)
                        {
                            case DateTimeFormat.MillisecondsSinceUnixEpoch:
                                var msTicksDouble = res * TicksPerMillisecond;
                                var msTicks = (long)msTicksDouble;
                                
                                if (msTicksDouble >= TimeSpan.MaxValue.Ticks)
                                {
                                    msTicks = TimeSpan.MaxValue.Ticks;
                                }
                                
                                if(msTicksDouble <= TimeSpan.MinValue.Ticks)
                                {
                                    msTicks = TimeSpan.MinValue.Ticks;
                                }

                                result = new TimeSpan(msTicks);
                                return true;
                            case DateTimeFormat.SecondsSinceUnixEpoch:
                                var sTicksDouble = res * TicksPerSecond;
                                var sTicks = (long)sTicksDouble;
                                
                                if (sTicksDouble >= TimeSpan.MaxValue.Ticks)
                                {
                                    sTicks = TimeSpan.MaxValue.Ticks;
                                }
                                
                                if(sTicksDouble <= TimeSpan.MinValue.Ticks)
                                {
                                    sTicks = TimeSpan.MinValue.Ticks;
                                }

                                result = new TimeSpan(sTicks);
                                return true;
                            default:
                                result = null;
                                return false;
                        }
                    }
                    break;
                case JsonObjectType.Number:
                    if (returnType == typeof(double))
                    {
                        result = NumberValue;
                        return true;
                    }
                    if (returnType == typeof(float))
                    {
                        result = (float)NumberValue;
                        return true;
                    }
                    if (returnType == typeof(decimal))
                    {
                        result = (decimal)NumberValue;
                        return true;
                    }
                    if (returnType == typeof(byte))
                    {
                        result = (byte)NumberValue;
                        return true;
                    }
                    if (returnType == typeof(sbyte))
                    {
                        result = (sbyte)NumberValue;
                        return true;
                    }
                    if (returnType == typeof(short))
                    {
                        result = (short)NumberValue;
                        return true;
                    }
                    if (returnType == typeof(ushort))
                    {
                        result = (ushort)NumberValue;
                        return true;
                    }
                    if (returnType == typeof(int))
                    {
                        result = (int)NumberValue;
                        return true;
                    }
                    if (returnType == typeof(uint))
                    {
                        result = (uint)NumberValue;
                        return true;
                    }
                    if (returnType == typeof(long))
                    {
                        result = (long)NumberValue;
                        return true;
                    }
                    if (returnType == typeof(ulong))
                    {
                        result = (ulong)NumberValue;
                        return true;
                    }
                    if (returnType == typeof(DateTime))
                    {
                        var res = (long)NumberValue;
                        switch (Options.UseDateTimeFormat)
                        {
                            case DateTimeFormat.MillisecondsSinceUnixEpoch:
                                result = Methods.UnixEpoch + TimeSpan.FromMilliseconds(res);
                                return true;
                            case DateTimeFormat.SecondsSinceUnixEpoch:
                                result = Methods.UnixEpoch + TimeSpan.FromSeconds(res);
                                return true;
                            default:
                                result = null;
                                return false;
                        }
                    }
                    if (returnType == typeof(DateTimeOffset))
                    {
                        var res = (long)NumberValue;
                        switch (Options.UseDateTimeFormat)
                        {
                            case DateTimeFormat.MillisecondsSinceUnixEpoch:
                                result = Methods.UnixEpochOffset + TimeSpan.FromMilliseconds(res);
                                return true;
                            case DateTimeFormat.SecondsSinceUnixEpoch:
                                result = Methods.UnixEpochOffset + TimeSpan.FromSeconds(res);
                                return true;
                            default:
                                result = null;
                                return false;
                        }
                    }
                    if (returnType == typeof(TimeSpan))
                    {
                        const double TicksPerMillisecond = 10000;
                        const double TicksPerSecond = 10000000;

                        var res = NumberValue;
                        switch (Options.UseDateTimeFormat)
                        {
                            case DateTimeFormat.MillisecondsSinceUnixEpoch:
                                var msTicksDouble = res * TicksPerMillisecond;
                                var msTicks = (long)msTicksDouble;

                                if (msTicksDouble >= TimeSpan.MaxValue.Ticks)
                                {
                                    msTicks = TimeSpan.MaxValue.Ticks;
                                }

                                if (msTicksDouble <= TimeSpan.MinValue.Ticks)
                                {
                                    msTicks = TimeSpan.MinValue.Ticks;
                                }

                                result = new TimeSpan(msTicks);
                                return true;
                            case DateTimeFormat.SecondsSinceUnixEpoch:
                                var sTicksDouble = res * TicksPerSecond;
                                var sTicks = (long)sTicksDouble;

                                if (sTicksDouble >= TimeSpan.MaxValue.Ticks)
                                {
                                    sTicks = TimeSpan.MaxValue.Ticks;
                                }

                                if (sTicksDouble <= TimeSpan.MinValue.Ticks)
                                {
                                    sTicks = TimeSpan.MinValue.Ticks;
                                }

                                result = new TimeSpan(sTicks);
                                return true;
                            default:
                                result = null;
                                return false;
                        }
                    }
                    break;
                case JsonObjectType.String:
                    if (returnType == typeof(string))
                    {
                        result = StringValue;
                        return true;
                    }
                    if (returnType.IsEnum())
                    {
                        if (returnType.IsFlagsEnum())
                        {
                            return ParseFlagsEnum(returnType, out result);
                        }

                        return EnumValues.TryParse(returnType, StringValue, out result);
                    }
                    if (returnType == typeof(Guid))
                    {
                        Guid guid;
                        if (!Guid.TryParseExact(StringValue, "D", out guid))
                        {
                            result = null;
                            return false;
                        }

                        result = guid;
                        return true;
                    }
                    if (returnType == typeof(DateTime))
                    {
                        DateTime res;
                        bool ret;

                        switch(Options.UseDateTimeFormat)
                        {
                            case DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch:
                                ret = Methods.ReadMicrosoftStyleDateTime(StringValue, out res);
                                result = res;
                                return ret;
                            case DateTimeFormat.ISO8601:
                                ret = Methods.ReadISO8601DateTime(StringValue, out res);
                                result = res;
                                return ret;
                            case DateTimeFormat.RFC1123:
                                ret = Methods.ReadRFC1123DateTime(StringValue, out res);
                                result = res;
                                return ret;
                            default:
                                result = null;
                                return false;
                        }
                    }
                    if (returnType == typeof(DateTimeOffset))
                    {
                        DateTime dt;
                        DateTimeOffset res;
                        bool ret;

                        switch (Options.UseDateTimeFormat)
                        {
                            case DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch:
                                ret = Methods.ReadMicrosoftStyleDateTime(StringValue, out dt);
                                res = dt;
                                result = res;
                                return ret;
                            case DateTimeFormat.ISO8601:
                                ret = Methods.ReadISO8601DateWithOffset(StringValue, out res);
                                result = res;
                                return ret;
                            default:
                                result = null;
                                return false;
                        }
                    }
                    if(returnType == typeof(TimeSpan))
                    {
                        TimeSpan ts;
                        bool ret;

                        switch (Options.UseDateTimeFormat)
                        {
                            case DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch:
                                ret = Methods.ReadMicrosoftStyleTimeSpan(StringValue, out ts);
                                result = ts;
                                return ret;
                            case DateTimeFormat.ISO8601:
                                ret = Methods.ReadISO8601TimeSpan(StringValue, out ts);
                                result = ts;
                                return ret;
                            default:
                                result = null;
                                return false;
                        }
                    }
                    break;
                case JsonObjectType.Object:
                    if (returnType == typeof(System.Collections.IEnumerable))
                    {
                        result = EnumerableObjectWrapper.MakeAsIEnumerable(ObjectMembers);
                        return true;
                    }

                    if (returnType.IsGenericDictionary())
                    {
                        var args = returnType.GetGenericArguments();
                        var keyType = args[0];
                        var valType = args[1];

                        var stringKeys = keyType == typeof(string);
                        var enumKeys = keyType.IsEnum();

                        // only strings and enums can be keys
                        if (!(stringKeys || enumKeys))
                        {
                            result = null;
                            return false;
                        }

                        var coerced = new Dictionary<object, object>(ObjectMembers.Count);
                        foreach (var kv in ObjectMembers)
                        {
                            object innerResult = null;
                            if (kv.Value != null && !kv.Value.InnerTryConvert(valType, out innerResult))
                            {
                                result = null;
                                return false;
                            }

                            if (stringKeys)
                            {
                                coerced[kv.Key] = innerResult;
                            }
                            else
                            {
                                object @enum = Enum.Parse(keyType, kv.Key, ignoreCase: true);
                                coerced[@enum] = innerResult;
                            }
                        }

                        if (stringKeys)
                        {
                            result = Utils.ProjectStringDictionary(coerced, valType);
                        }
                        else
                        {
                            // enum keys
                            result = Utils.ProjectEnumDictionary(coerced, keyType, valType);
                        }

                        return true;
                    }
                    break;
                case JsonObjectType.Array:
                    if (returnType == typeof(System.Collections.IEnumerable))
                    {
                        result = EnumerableArrayWrapper.MakeAsIEnumerable(ArrayValue);
                        return true;
                    }

                    if (returnType.IsGenericEnumerable())
                    {
                        var castTo = returnType.GetGenericArguments()[0];

                        if (castTo == typeof(object))
                        {
                            result = EnumerableArrayWrapper.MakeAsIEnumerableOfT(ArrayValue);
                            return true;
                        }

                        bool bail = false;

                        var dynamicProjection =
                            ArrayValue.Select(
                                val =>
                                {
                                    object innerResult;
                                    if (!val.InnerTryConvert(castTo, out innerResult))
                                    {
                                        bail = true;
                                        return Activator.CreateInstance(castTo);
                                    }

                                    return innerResult;
                                }
                            );

                        result = Utils.DynamicProject(dynamicProjection, castTo);

                        if (bail)
                        {
                            result = null;
                            return false;
                        }

                        return true;
                    }
                    break;
            }

            result = null;
            return false;
        }

        bool ParseFlagsEnum(Type enumType, out object result)
        {
            result = Activator.CreateInstance(enumType);

            var ix = 0;
            int iy;
            while (ix != -1)
            {
                iy = StringValue.IndexOf(',', ix);
                var takeTil = iy != -1 ? iy : StringValue.Length;

                var asStr = StringValue.Substring(ix, takeTil - ix);

                object parsed;
                if (!EnumValues.TryParse(enumType, asStr, out parsed))
                {
                    result = null;
                    return false;
                }

                FlagsEnumCombiner.CombineInPlace(enumType, parsed, result);

                ix = iy;

                if (ix != -1)
                {
                    // skip ,
                    ix++;

                    // skip very specific whitespace
                    while (StringValue[ix] == ' ' && ix < StringValue.Length) ix++;
                }
            }

            return true;
        }

        static MethodInfo InnerTryInvokeMember = typeof(JsonObject).GetMethod("_InnerTryInvokeMember", BindingFlags.NonPublic | BindingFlags.Instance);
        bool _InnerTryInvokeMember(string name, object[] args, out object result)
        {
            if (name == "ToString" && args.Length == 0)
            {
                result = this.ToString();
                return true;
            }

            if (Type == JsonObjectType.Object)
            {
                if (name == "GetEnumerator" && args.Length == 0)
                {
                    result = ObjectEnumeratorWrapper.MakeAsIEnumerator(ObjectMembers.GetEnumerator());
                    return true;
                }

                if(name == "ContainsKey" && args.Length == 1)
                {
                    var key = args[0] as string;
                    if(key == null)
                    {
                        result = null;
                        return false;
                    }

                    result = ObjectMembers.ContainsKey(key);
                    return true;
                }

                result = null;
                return false;
            }

            if (Type == JsonObjectType.Array)
            {
                if (name == "GetEnumerator" && args.Length == 0)
                {
                    result = ArrayEnumeratorWrapper.MakeAsIEnumerator(ArrayValue.GetEnumerator());
                    return true;
                }

                result = null;
                return false;
            }

            result = null;
            return false;
        }
    }
}

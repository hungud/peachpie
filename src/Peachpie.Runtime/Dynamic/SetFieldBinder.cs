﻿using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using Pchp.CodeAnalysis.Semantics;
using Pchp.Core.Reflection;

namespace Pchp.Core.Dynamic
{
    public class SetFieldBinder : DynamicMetaObjectBinder
    {
        readonly string _name;
        readonly Type _classContext;
        readonly AccessMask _access;

        public SetFieldBinder(string name, RuntimeTypeHandle classContext, AccessMask access)
        {
            _name = name;
            _classContext = Type.GetTypeFromHandle(classContext);
            _access = access & AccessMask.WriteMask;
        }

        void ResolveArgs(DynamicMetaObject[] args, ref BindingRestrictions restrictions, out string fieldName, out Expression valueExpr)
        {
            int i = 0;

            // name
            if (_name != null)
            {
                fieldName = _name;
            }
            else
            {
                Debug.Assert(args.Length >= i && args[i].LimitType == typeof(string));
                restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Equal(args[i].Expression, Expression.Constant(args[i].Value)))); // args[0] == "VALUE"
                fieldName = (string)args[i++].Value;
            }

            //
            valueExpr = (args.Length > i && i < args.Length - 1) ? args[i].Expression : null;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var restrictions = BindingRestrictions.Empty;
            
            Expression target_expr;
            PhpTypeInfo phptype;

            //
            string fldName;
            Expression value;

            ResolveArgs(args, ref restrictions, out fldName, out value);
            var ctx = args[args.Length - 1];

            //
            if (target.LimitType == typeof(PhpTypeInfo))    // static field
            {
                target_expr = null;
                phptype = (PhpTypeInfo)target.Value;
            }
            else
            {
                var isobject = BinderHelpers.TryTargetAsObject(target, out DynamicMetaObject instance);
                if (isobject == false)
                {
                    throw new NotSupportedException();
                }
                
                restrictions = restrictions.Merge(instance.Restrictions);
                target_expr = Expression.Convert(instance.Expression, instance.RuntimeType);
                phptype = instance.RuntimeType.GetPhpTypeInfo();
            }

            //
            var setter = BinderHelpers.BindField(phptype, _classContext, target_expr, fldName, ctx.Expression, _access, value);
            if (setter != null)
            {
                //
                return new DynamicMetaObject(setter, restrictions);
            }

            // field not found
            throw new NotImplementedException();
        }
    }
}

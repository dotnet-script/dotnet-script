using System;
using System.Reflection;

namespace Dotnet.Script.Core.Internal
{
    internal static class ReflectionExtensions
    {
        static MethodInfo RequireInstanceMethod(this TypeInfo typeInfo, string name, BindingFlags bindingFlags)
        {
            if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));
            var method = typeInfo.GetMethod(name, bindingFlags | BindingFlags.Instance);
            if (method == null)
                throw new MissingMemberException($"{typeInfo.FullName} has no instance method named {name}.");
            return method;
        }

        // Strong-typed CreateDelegate and Invoke versions

        // For methods with 0 arguments

        public static TResult Invoke<TResult>(this object target, string name, BindingFlags bindingFlags) =>
            target.Invoke<TResult>(t => t.RequireInstanceMethod(name, bindingFlags));

        public static TResult Invoke<TResult>(this object target, Func<TypeInfo, MethodInfo> methodSelector) =>
            methodSelector(target.GetType().GetTypeInfo()).Invoke<TResult>(target);

        public static TResult Invoke<TResult>(this MethodInfo method, object target) =>
            ((Func<TResult>) method.CreateDelegate(typeof(Func<TResult>), target))();

        public static Func<TResult> CreateDelegate<TResult>(this object target, string name, BindingFlags bindingFlags) =>
            target.CreateDelegate<TResult>(t => t.RequireInstanceMethod(name, bindingFlags));

        public static Func<TResult> CreateDelegate<TResult>(this object target, Func<TypeInfo, MethodInfo> methodSelector) =>
            methodSelector(target.GetType().GetTypeInfo()).CreateDelegate<TResult>(target);

        public static Func<TResult> CreateDelegate<TResult>(this MethodInfo method, object target) =>
            (Func<TResult>)method.CreateDelegate(typeof(Func<TResult>), target);

        // For methods with 1 argument

        public static TResult Invoke<T, TResult>(this object target, string name, BindingFlags bindingFlags, T arg) =>
            target.Invoke<T, TResult>(t => t.RequireInstanceMethod(name, bindingFlags), arg);

        public static TResult Invoke<T, TResult>(this object target, Func<TypeInfo, MethodInfo> methodSelector, T arg) =>
            methodSelector(target.GetType().GetTypeInfo()).Invoke<T, TResult>(target, arg);

        public static TResult Invoke<T, TResult>(this MethodInfo method, object target, T arg) =>
            ((Func<T, TResult>)method.CreateDelegate(typeof(Func<T, TResult>), target))(arg);

        public static Func<T, TResult> CreateDelegate<T, TResult>(this object target, string name, BindingFlags bindingFlags) =>
            target.CreateDelegate<T, TResult>(t => t.RequireInstanceMethod(name, bindingFlags));

        public static Func<T, TResult> CreateDelegate<T, TResult>(this object target, Func<TypeInfo, MethodInfo> methodSelector) =>
            methodSelector(target.GetType().GetTypeInfo()).CreateDelegate<T, TResult>(target);

        public static Func<T, TResult> CreateDelegate<T, TResult>(this MethodInfo method, object target) =>
            (Func<T, TResult>)method.CreateDelegate(typeof(Func<T, TResult>), target);

        // For methods with 2 arguments

        public static TResult Invoke<T1, T2, TResult>(this object target, string name, BindingFlags bindingFlags, T1 arg1, T2 arg2) =>
            target.Invoke<T1, T2, TResult>(t => t.RequireInstanceMethod(name, bindingFlags), arg1, arg2);

        public static TResult Invoke<T1, T2, TResult>(this object target, Func<TypeInfo, MethodInfo> methodSelector, T1 arg1, T2 arg2) =>
            methodSelector(target.GetType().GetTypeInfo()).Invoke<T1, T2, TResult>(target, arg1, arg2);

        public static TResult Invoke<T1, T2, TResult>(this MethodInfo method, object target, T1 arg1, T2 arg2) =>
            ((Func<T1, T2, TResult>)method.CreateDelegate(typeof(Func<T1, T2, TResult>), target))(arg1, arg2);

        public static Func<T1, T2, TResult> CreateDelegate<T1, T2, TResult>(this object target, string name, BindingFlags bindingFlags) =>
            target.CreateDelegate<T1, T2, TResult>(t => t.RequireInstanceMethod(name, bindingFlags));

        public static Func<T1, T2, TResult> CreateDelegate<T1, T2, TResult>(this object target, Func<TypeInfo, MethodInfo> methodSelector) =>
            methodSelector(target.GetType().GetTypeInfo()).CreateDelegate<T1, T2, TResult>(target);

        public static Func<T1, T2, TResult> CreateDelegate<T1, T2, TResult>(this MethodInfo method, object target) =>
            (Func<T1, T2, TResult>)method.CreateDelegate(typeof(Func<T1, T2, TResult>), target);
    }
}
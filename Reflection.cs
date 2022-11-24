using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AgdaLibraryLookup
{
    public static class Reflection
    {
        public static IEnumerable<Type> FindAllDerivedTypes<T>()
            => FindAllDerivedTypes<T>(Assembly.GetAssembly(typeof(T))!);

        public static IEnumerable<Type> FindAllDerivedTypes<T>(Assembly assembly)
        {
            var derivedType = typeof(T);
            return assembly
                .GetTypes()
                .Where(t => t != derivedType && derivedType.IsAssignableFrom(t));
        }

        public static IEnumerable<(Type Type, T Attr)> FindAllAttributes<T>() where T : System.Attribute
            => FindAllAttributes<T>(Assembly.GetAssembly(typeof(T))!);

        public static IEnumerable<(Type Type, T Attr)> FindAllAttributes<T>(Assembly assembly) where T : System.Attribute
        {
            return assembly
                    .GetTypes()
                    .Select(t => (Type: t, Attr: t.GetCustomAttribute<T>()))
                    .Where(te => te.Attr is not null)!;
        }

        //public static Type? CreateSingleton<T>(ModuleBuilder mb, string name, T value)
        //{ 
        //    TypeBuilder tb = mb.DefineType(name, TypeAttributes.Public);
        //    FieldBuilder fb = tb.DefineField(
        //        "Value", 
        //        typeof(T), 
        //        FieldAttributes.Public | FieldAttributes.Static
        //    );
        //    fb.SetValue(null, value);
        //    return tb.CreateType();
        //}

        //public static T GetSingletonValue<T>()
    }
}

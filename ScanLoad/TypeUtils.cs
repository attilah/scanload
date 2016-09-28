using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScanLoad
{
    internal static class TypeUtils
    {
        public static IEnumerable<TypeInfo> GetDefinedTypes(Assembly assembly)
        {
            try
            {
                return assembly.DefinedTypes;
            }
            catch (Exception exception)
            {
                //if (logger != null && logger.IsWarning)
                //{
                //    var message = $"AssemblyLoader encountered an exception loading types from assembly '{assembly.FullName}': {exception}";
                //    logger.Warn(ErrorCode.Loader_TypeLoadError_5, message, exception);
                //}

                var typeLoadException = exception as ReflectionTypeLoadException;
                if (typeLoadException != null)
                {
                    return typeLoadException.Types?.Where(type => type != null).Select(type => type.GetTypeInfo()) ??
                           Enumerable.Empty<TypeInfo>();
                }

                return Enumerable.Empty<TypeInfo>();
            }
        }

        public static Type ToReflectionOnlyType(Type type)
        {
            return type.Assembly.ReflectionOnly ? type : ResolveReflectionOnlyType(type.AssemblyQualifiedName);
        }

        public static Type ResolveReflectionOnlyType(string assemblyQualifiedName)
        {
            //return CachedReflectionOnlyTypeResolver.Instance.ResolveType(assemblyQualifiedName);
            return null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScanLoad
{
    internal static class Utils
    {
        public static AggregateException Flatten(this ReflectionTypeLoadException rtle)
        {
            // if ReflectionTypeLoadException is thrown, we need to provide the
            // LoaderExceptions property in order to make it meaningful.
            var all = new List<Exception> { rtle };
            all.AddRange(rtle.LoaderExceptions);
            throw new AggregateException("A ReflectionTypeLoadException has been thrown. The original exception and the contents of the LoaderExceptions property have been aggregated for your convenience.", all);
        }
    }
}

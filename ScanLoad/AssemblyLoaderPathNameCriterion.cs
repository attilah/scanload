﻿using System.Collections.Generic;

namespace ScanLoad
{
    using System;

    internal class AssemblyLoaderPathNameCriterion : AssemblyLoaderCriterion
    {
        internal new delegate bool Predicate(string pathName, out IEnumerable<string> complaints);

        internal static AssemblyLoaderPathNameCriterion NewCriterion(Predicate predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            return new AssemblyLoaderPathNameCriterion(predicate);
        }

        private AssemblyLoaderPathNameCriterion(Predicate predicate) :
            base((object input, out IEnumerable<string> complaints) =>
                    predicate((string)input, out complaints))
        { }
    }
}

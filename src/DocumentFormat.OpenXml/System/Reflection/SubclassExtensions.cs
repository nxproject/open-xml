// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace DocumentFormat.OpenXml
{
    internal static class SubclassExtensions
    {
        public static IEnumerable<Type> GetTypes(this Assembly assembly)
        {
            foreach (var type in assembly.DefinedTypes)
            {
                yield return type.AsType();
            }
        }

        /// <summary>
        /// .NET Standard 1.3 does not have an equivalent API for this, but it is fairly
        /// trivial to replicate
        /// </summary>
        public static bool IsSubclassOf(this Type p, Type c)
        {
            if (p == c)
            {
                return false;
            }

            while (p != null)
            {
                if (p == c)
                {
                    return true;
                }

                p = p.GetTypeInfo().BaseType;
            }

            return false;
        }
    }
}

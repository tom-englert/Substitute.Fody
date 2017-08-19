// This is test code, no warnings please...
// ReSharper disable All
#pragma warning disable 

using System.Collections;
using System.Collections.Generic;

using AssemblyToProcess;

using Substitute;

[assembly: Substitute(typeof(WithExplicitInterfaces), typeof(SubstituteWithExplicitInterfaces))]

#if MISSING_INTERFACES
[assembly: Substitute(typeof(WithDerivedInterfaces), typeof(SubstituteWithLessDerivedInterfaces))]
#else
[assembly: Substitute(typeof(WithDerivedInterfaces), typeof(SubstituteWithDerivedInterfaces))]
#endif

#if DUPLICATE
[assembly: Substitute(typeof(WithDerivedInterfaces), typeof(SubstituteWithLessDerivedInterfaces))]
#endif

#if RECURSION
[assembly: Substitute(typeof(SubstituteWithExplicitInterfaces), typeof(SubstituteWithDerivedInterfaces))]
#endif

namespace AssemblyToProcess
{
    public class WithExplicitInterfaces : IEnumerable<string>
    {
        private List<string> _inner = new List<string> { "a", "b", "c" };

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }
    }

    public class WithDerivedInterfaces : List<string>
    {
        public WithDerivedInterfaces()
            : base(new[] { "a", "b", "c" })
        {

        }
    }

    public class SubstituteWithExplicitInterfaces : IEnumerable<string>
    {
        private List<string> _inner = new List<string> { "A", "B", "C" };

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }
    }

    public class SubstituteWithDerivedInterfaces : List<string>
    {
        public SubstituteWithDerivedInterfaces()
            : base(new[] { "A", "B", "C" })
        {

        }
    }

    public class SubstituteWithLessDerivedInterfaces : HashSet<string>
    {
        public SubstituteWithLessDerivedInterfaces()
            : base(new[] { "A", "B", "C" })
        {

        }
    }

    public class UsageOfInterface
    {
        private WithExplicitInterfaces _explicit = new WithExplicitInterfaces();
        private WithDerivedInterfaces _derived = new WithDerivedInterfaces();

        public IEnumerable<string> Explicit
        {
            get
            {
                foreach (var item in _explicit)
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<string> Derived
        {
            get
            {
                foreach (var item in _derived)
                {
                    yield return item;
                }
            }
        }
    }


}

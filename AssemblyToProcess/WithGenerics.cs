// This is test code, no warnings please...
// ReSharper disable All
#pragma warning disable 

using System.Collections.Generic;

namespace AssemblyToProcess
{
    public class WithGenerics
    {
        private IList<WithExplicitInterfaces> _list;

        public WithGenerics()
        {
            _list = new List<WithExplicitInterfaces> { new WithExplicitInterfaces() };
        }

        public WithExplicitInterfaces Item => _list[0];

        public T GetValue<T>() where T : WithExplicitInterfaces
        {
            return (T)Item;
        }
    }

    public class GenericClass<T> where T : WithExplicitInterfaces
    {
        private WithGenerics _inner = new WithGenerics();

        public T GetValue<T>() where T : WithExplicitInterfaces
        {
            return _inner.GetValue<T>();
        }
    }
}

using SbcLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SbcCore
{
    [ImplementClass(typeof(IDisposable))]
    public interface System_IDisposable
    {
        void Dispose();
    }

    [ImplementClass(typeof(IEnumerable))]
    public interface System_Collections_IEnumerable : IDisposable
    {
        // Returns an IEnumerator for this enumerable Object.  The enumerator provides
        // a simple way to access all the contents of a collection.
        IEnumerator GetEnumerator();
    }

    [ImplementClass(typeof(IEnumerable<>))]
    public interface System_Collections_Generic_IEnumerable<T> : IEnumerable
    {
        // Returns an IEnumerator for this enumerable Object.  The enumerator provides
        // a simple way to access all the contents of a collection.
        new IEnumerator<T> GetEnumerator();
    }

    [ImplementClass(typeof(IEnumerator))]
    public interface System_Collections_IEnumerator
    {
        // Interfaces are not serializable
        // Advances the enumerator to the next element of the enumeration and
        // returns a boolean indicating whether an element is available. Upon
        // creation, an enumerator is conceptually positioned before the first
        // element of the enumeration, and the first call to MoveNext 
        // brings the first element of the enumeration into view.
        bool MoveNext();

        // Returns the current element of the enumeration. The returned value is
        // undefined before the first call to MoveNext and following a
        // call to MoveNext that returned false. Multiple calls to
        // GetCurrent with no intervening calls to MoveNext 
        // will return the same object.
        object Current { get; }

        // Resets the enumerator to the beginning of the enumeration, starting over.
        // The preferred behavior for Reset is to return the exact same enumeration.
        // This means if you modify the underlying collection then call Reset, your
        // IEnumerator will be invalid, just as it would have been if you had called
        // MoveNext or Current.
        void Reset();
    }

    [ImplementClass(typeof(IEnumerator<>))]
    public interface System_Collections_Generic_IEnumerator<T> : IDisposable, System_Collections_IEnumerator
    {
        // Returns the current element of the enumeration. The returned value is
        // undefined before the first call to MoveNext and following a
        // call to MoveNext that returned false. Multiple calls to
        // GetCurrent with no intervening calls to MoveNext 
        // will return the same object.
        new T Current { get; }
    }
}

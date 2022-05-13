/* SerializationContext
 * MiddleVR
 * (c) MiddleVR
 */

#if !ENABLE_IL2CPP
using System.Collections.Generic;

namespace MiddleVR.Unity.Serialization
{
    /// <summary>
    /// Stack of object references to avoid reference cycles.
    /// </summary>
    public class SerializationContext
    {
        internal Stack<object> _references = new Stack<object>();
        internal bool Contains(object o) => _references.Contains(o);
        internal void Push(object o) => _references.Push(o);
        internal void Pop() => _references.Pop();
        internal void Reset() => _references.Clear();
    }
}
#endif

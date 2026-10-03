#nullable enable

using System;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    /// <summary>
    /// Indicates that calling this method triggers network replication (e.g. via RPCs or network state changes).
    /// Particularly useful for polymorphic or interface methods whose names cannot follow standard RPC naming conventions (e.g. ...Rpc).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ReplicatedMethodAttribute : Attribute
    {
        /// <summary>
        /// Indicates whether non-owner clients are allowed to invoke this replicated method.
        /// Defaults to <c>false</c> (only the owner and the server are permitted to invoke it).
        /// </summary>
        public bool AllowNonOwner { get; set; }

        public ReplicatedMethodAttribute(bool allowNonOwner = false)
        {
            AllowNonOwner = allowNonOwner;
        }
    }
}

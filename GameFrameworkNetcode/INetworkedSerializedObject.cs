using GameFramework.Dependencies;

namespace GameFramework.Saving
{
    public interface INetworkedSerializedObject : ISerializedObject
    {
        /// <summary>
        /// Should Serialize and Send the current state of the object to a specific client.
        /// Generally done with a network message (e.g., RPC) to the client identified by clientId.
        /// Mainly used we new clients join the game and need to get the current state of existing objects.
        /// After that, we should rely on smaller, incremental updates to keep clients in sync.
        /// </summary>
        /// <param name="clientId">The client identifier to send the state to.</param>
        void SendStateToClient(ulong clientId);
    }
}
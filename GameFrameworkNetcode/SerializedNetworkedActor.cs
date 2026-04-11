using GameFramework.Saving;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    /// <summary>
    /// A networked actor that can serialize and deserialize its state for network transmission and game saving.
    /// </summary>
    /// <typeparam name="T">The type representing the state of the actor.</typeparam>
    public abstract class SerializedNetworkedActor<T> : NetworkedActor, INetworkedSerializedObject
    {
        public string Serialize()
        {
            return JsonConvert.SerializeObject(GetState(), Formatting.Indented);
        }

        public void Deserialize(string serializedData)
        {
            if (string.IsNullOrEmpty(serializedData))
            {
                Debug.LogError("Serialized data is null or empty.");
                return;
            }

            try
            {
                T state = JsonConvert.DeserializeObject<T>(serializedData);
                SetState(state);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to deserialize data: {ex.Message}");
            }
        }

        public void SendStateToClientFromServer(ulong clientId)
        {
            if(!IsServer) return;
            string stateJson = Serialize();
            SendSerializedStateRpc(stateJson, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        public void SendStateToAllClientsFromServer()
        {
            if(!IsServer) return;
            string stateJson = Serialize();
            SendSerializedStateRpc(stateJson);
        }

        public void SendStateToServerFromClient()
        {
            if(!IsClient) return;
            string stateJson = Serialize();
            SendSerializedStateRpc(stateJson, RpcTarget.Server);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SendSerializedStateRpc(string serializedData, RpcParams rpcParams = default)
        {
            Deserialize(serializedData);
            if (IsServer)
            {
                ulong senderId = rpcParams.Receive.SenderClientId;
                // Relay to all clients EXCEPT the sender
                // We use Not(senderId) to prevent jitter/redundant updates on the originating client
                SendSerializedStateRpc(serializedData, RpcTarget.Not(senderId, RpcTargetUse.Temp));
            }
        }

        protected abstract T GetState();
        protected abstract void SetState(T state);
    }
}
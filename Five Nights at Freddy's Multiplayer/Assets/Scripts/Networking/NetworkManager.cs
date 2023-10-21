using QFSW.QC;
using Riptide;
using Riptide.Transports;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager instance_;
    public static NetworkManager instance
    {
        get { return instance_; }
        set
        {
            if (instance_ == null)
            {
                instance_ = value;
            }
        }
    }

    [SerializeField] private Client client;
    [SerializeField] private Server server;
    [SerializeField] private NetworkType type = NetworkType.NONE;
    private Dictionary<ushort, Player> players;
    [SerializeField] private bool isDebugging;

    private string clientUsername;

    public enum NetworkType : ushort
    {
        NONE = 0,
        CLIENT = 1,
        SERVER = 2
    }

    #region Start/Stop Client/Server
    public void StartClient(string ip_, string port_, string username_)
    {
        SceneManager.LoadScene(1);
        if (server != null) { server = null; }
        if (client != null)
        {
            Debug.LogWarning("[CLIENT]: Client is already active!");
        }

        clientUsername = username_;

        client = new Client();
        client.Connected += OnLocalConnectToServer_Client;
        client.Connect(ip_ + ":" + port_);
    }

    public void StartServer(string port_, string username_)
    {
        if (client != null) { client = null; }
        if (server != null)
        {
            Debug.LogWarning("[SERVER]: Server is already active!");
        }

        type = NetworkType.SERVER;
        server = new Server();
        server.ClientDisconnected += OnPlayerDisconnect_Server;

        clientUsername = username_;

        if (ushort.TryParse(port_, out ushort portS_)) { server.Start(portS_, 20); SceneManager.LoadScene(1); }
        else
        {
            // -- TODO: Make ERROR Notifications for Errors.
            Debug.LogError("[SERVER]: Server port was unable to be parsed into a ushort variable.");
            QuantumConsole.Instance.LogToConsole("[SERVER]: Server port was unable to be parsed into a ushort variable.");
        }
        AddLocalPlayer(username_);
    }

    public void StopClient()
    {
        if (type == NetworkType.CLIENT)
        {
            client.Disconnect();
        }
    }
    public void StopServer()
    {
        if (type == NetworkType.SERVER)
        {
            server.Stop();
        }
    }
    #endregion

    #region Functions
    public void Awake()
    {
        instance = this;
        Riptide.Utils.RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, true);
    }
    public void FixedUpdate()
    {
        if (client != null)
        {
            client.Update();
        }

        if (server != null)
        {
            server.Update();
        }
    }

    public void AddLocalPlayer(string username_)
    {
        if (type == NetworkType.SERVER)
        {
            players.Add(0, Instantiate(Resources.Load<GameObject>("Prefabs/Player")).GetComponent<Player>());
            players[0].SetUsername(username_);
            players[0].gameObject.name = $"[0] {username_} (Local Player)";
        }
        else if (type == NetworkType.CLIENT)
        {
            players.Add(client.Id, Instantiate(Resources.Load<GameObject>("Prefabs/Player")).GetComponent<Player>());
            players[client.Id].SetUsername(username_);
            players[client.Id].gameObject.name = $"[{client.Id}] {username_} (Local Player)";
        }
    }
    public void AddPlayer(ushort id_, string username_)
    {
        if (type == NetworkType.SERVER && id_ == 0)
        {
            AddLocalPlayer(username_);
            return;
        }
        else if (type == NetworkType.CLIENT && client.Id == id_)
        {
            AddLocalPlayer(username_);
            return;
        }

        players.Add(id_, Instantiate(Resources.Load<GameObject>("Prefabs/Player")).GetComponent<Player>());
        players[id_].SetUsername(username_);
        players[id_].gameObject.name = $"[{id_}] {username_}";
    }

    public void Log(string log_, bool isServer_ = false, bool requiresDebugging_ = false)
    {
        if (requiresDebugging_ && isDebugging)
        {
            if (isServer_)
            {
                log_ = "[SERVER]: " + log_;
            }
            else
            {
                log_ = "[CLIENT]: " + log_;
            }

            Debug.Log(log_);
            QuantumConsole.Instance.LogToConsole(log_);
        }
    }

    public void LogWarning(string log_, bool isServer_ = false, bool requiresDebugging_ = false)
    {
        if (requiresDebugging_ && isDebugging)
        {
            if (isServer_)
            {
                log_ = "[SERVER]: " + log_;
            }
            else
            {
                log_ = "[CLIENT]: " + log_;
            }

            Debug.LogWarning(log_);
            QuantumConsole.Instance.LogToConsole(log_);
        }
    }

    public void LogError(string log_, bool isServer_ = false, bool requiresDebugging_ = false)
    {
        if (requiresDebugging_ && isDebugging)
        {
            if (isServer_)
            {
                log_ = "[SERVER]: " + log_;
            }
            else
            {
                log_ = "[CLIENT]: " + log_;
            }

            Debug.LogError(log_);
            QuantumConsole.Instance.LogToConsole(log_);
        }
    }
    #endregion

    #region Events
    private void OnLocalConnectToServer_Client(object sender, System.EventArgs e)
    {
        SceneManager.LoadScene(1);
        AddLocalPlayer(clientUsername);
    }


    private void OnPlayerDisconnect_Server(object sender, ServerDisconnectedEventArgs e)
    {
        Destroy(players[e.Client.Id].gameObject);
        players.Remove(e.Client.Id);
    }
    #endregion

    public class Packets
    {
        private enum ServerToClient : ushort
        {
            // -- INIT --
            SendClientInfoToAll = 0, // Used for INIT and possibly changes.
            SendAllToNewClient = 1,


        }

        private enum ClientToServer : ushort
        {
            // -- INIT --
            SendInfoToServer = 0,
        }

        /////////////////////////////////////////////////////////////////////
        //                             INFORMATION                         //
        /////////////////////////////////////////////////////////////////////
        // Each method has an end to it which means different things.      //
        // _CTS tells you that this is a CLIENT to SERVER method.          //
        // _STC is the same as _CTS except it's SERVER to CLIENT.          //
        // _CHANDLE is a method to receive information from the SERVER.    //
        // _SHANDLE is like _CHANDLE except handling info from the CLIENT. //
        /////////////////////////////////////////////////////////////////////

        #region INIT 
        public static void SendClientInfo_CTS()
        {
            if (instance.type == NetworkType.CLIENT)
            {
                Message msg_ = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServer.SendInfoToServer);
                msg_.AddString(instance.players[instance.client.Id].GetUsername());
                instance.client.Send(msg_);
            }
        }
        [MessageHandler((ushort)ClientToServer.SendInfoToServer)]
        private static void ReceiveClientInfo_SHANDLE(ushort id_, Message msg_)
        {
            if (instance.type == NetworkType.SERVER)
            {
                instance.AddPlayer(id_, msg_.GetString());
                SendClientInfoToAll_STC(id_);
            }
        }

        public static void SendClientInfoToAll_STC(ushort id_)
        {
            if (instance.type == NetworkType.SERVER)
            {
                Message msg_ = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClient.SendClientInfoToAll);
                msg_.AddUShort(id_);
                msg_.AddString(instance.players[id_].GetUsername());
                instance.server.SendToAll(msg_, id_);

                SendAllInfoToClient_STC(id_);
            }
        }
        [MessageHandler((ushort)ServerToClient.SendClientInfoToAll)]
        private static void ReceiveNewClientInfo_CHANDLE(Message msg_)
        {
            if (instance.type == NetworkType.CLIENT)
            {
                instance.AddPlayer(msg_.GetUShort(), msg_.GetString());
            }
        }

        public static void SendAllInfoToClient_STC(int id_)
        {
            if (instance.type == NetworkType.SERVER)
            {
                Message msg_ = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClient.SendAllToNewClient);
                msg_.AddInt(instance.players.Count - 1);

                foreach (ushort key_ in instance.players.Keys)
                {
                    if (key_ != id_)
                    {
                        msg_.AddUShort(key_);
                        msg_.AddString(instance.players[key_].GetUsername());
                        //msg_.AddString(instance.players[key_].GetCharacter());
                    }
                }
            }
        }
        [MessageHandler((ushort)ServerToClient.SendAllToNewClient)]
        private static void ReceiveAllClientInfo_CHANDLE(Message msg_)
        {
            if (instance.type == NetworkType.CLIENT)
            {
                int count_ = msg_.GetInt();

                for (int x = 0; x < count_; x++)
                {
                    instance.AddPlayer(msg_.GetUShort(), msg_.GetString());
                    // -- TODO: Create Character Scripts
                }
            }
        }
        #endregion
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameObject connectCanvas;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;

    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField lobbyCodeField;

    [SerializeField] private TMP_Text errorText;

    private Lobby currentLobby;

    private void Start()
    {
        if (PlayerPrefs.HasKey("Username"))
            usernameField.text = PlayerPrefs.GetString("Username");

        _ = ConnectToRelay();
    }

    public void OnUsernameChange()
    {
        PlayerPrefs.SetString("Username", usernameField.text);
    }
    public void OnLobbyCodeChange()
    {
        PlayerPrefs.SetString("LobbyCode", lobbyCodeField.text);
    }

    private IEnumerator ErrorMessage(string newMessage)
    {
        errorText.text = newMessage;

        yield return new WaitForSeconds(3);

        if (errorText.text == newMessage)
            errorText.text = string.Empty;
    }

    private async Task ConnectToRelay() //run in Start
    {
        errorText.text = "Connecting...";

        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            StartCoroutine(ErrorMessage("Connected!"));

            ToggleEnterLobbyInterface(true);
        }
        catch
        {
            StartCoroutine(ErrorMessage("Connection failed. Please check your internet connection and restart the game"));
        }
    }

    // Keep lobby active (lobbies are automatically hidden after 30 seconds of inactivity)
    private IEnumerator HandleLobbyHeartbeat()
    {
        while (currentLobby != null)
        {
            SendHeartbeat();
            yield return new WaitForSeconds(15);
        }
    }
    private async void SendHeartbeat()
    {
        await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
    }


    private void ToggleEnterLobbyInterface(bool on)
    {
        createLobbyButton.interactable = on;
        joinLobbyButton.interactable = on;
        usernameField.interactable = on;
        lobbyCodeField.interactable = on;
    }


    public void SelectCreateLobby()
    {
        if (UsernameLobbyError())
            return;

        CreateLobby();
    }

    public void SelectJoinLobby()
    {
        if (UsernameLobbyError())
            return;

        JoinLobby();
    }

    private bool UsernameLobbyError()
    {
        if (usernameField.text == string.Empty)
        {
            StartCoroutine(ErrorMessage("Must choose a username!"));
            return true;
        }

        return false;
    }

    private bool LobbyCodeError()
    {
        if (lobbyCodeField.text == string.Empty)
        {
            StartCoroutine(ErrorMessage("Must choose a room name!"));
            return true;
        }

        return false;
    }


    public async void CreateLobby()
    {
        try
        {
            // Check for existing lobbies with the provided code

            QueryLobbiesOptions queryLobbiesOptions = new()
            {
                Count = 50,
                Filters = new List<QueryFilter>()
                {
                    new(field: QueryFilter.FieldOptions.S2, op: QueryFilter.OpOptions.EQ, value: "tempLobbyCode") //lobbyCodeField.text)
                }
            };
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            if (queryResponse.Results.Count != 0)
            {
                StartCoroutine(ErrorMessage("A lobby with that code already exists. Please choose another code"));
                //StartCoroutine(ErrorMessage("The old game lobby is still being deleted. Try again in a few seconds"));
                return;
            }

                // Create Lobby

            // Lobby is public by default
            int maxPlayers = 2;
            currentLobby = await LobbyService.Instance.CreateLobbyAsync("NewLobby", maxPlayers);

            Debug.Log("Created Lobby");

            StartCoroutine(HandleLobbyHeartbeat());

            int numberOfNonHostConnections = maxPlayers - 1;
            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(numberOfNonHostConnections);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(hostAllocation, "dtls"));

            NetworkManager.Singleton.StartHost();

            // Set up JoinAllocation
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);

            // Save JoinCode In Lobby Data
            try
            {
                // Update currentLobby
                currentLobby = await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject> //JoinCode = S1
                    {
                        // JoinCode is never displayed to the player--it's automatically generated by the server and used behind the scenes
                        // LobbyCode is the code provided by the player

                        { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode, DataObject.IndexOptions.S1) },
                        { "LobbyCode", new DataObject(DataObject.VisibilityOptions.Public, "tempLobbyCode", DataObject.IndexOptions.S2) }
                    }
                });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void JoinLobby()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new()
            {
                Count = 50
            };
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
      
            if (queryResponse.Results.Count == 0)
            {
                Debug.Log("No lobby found");
                return;
            }
      
            if (queryResponse.Results[0].AvailableSlots == 0)
            {
                Debug.Log("Lobby is already full");
                return;
            }
      
            if (queryResponse.Results[0].Data == null || !queryResponse.Results[0].Data.ContainsKey("JoinCode"))
            {
                // Data is null when no data values exist, such as a JoinCode
                // JoinCode is created when host is first connected to relay. It's possible to join the lobby before the relay connection
                // is established and before JoinCode is created
                Debug.Log("Lobby is still being created, trying again in 2 seconds");
                Invoke(nameof(JoinLobby), 2);
                return;
            }
      
            currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
      
            Debug.Log("Joined Lobby");
      
            string joinCode = currentLobby.Data["JoinCode"].Value;
      
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
      
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public override void OnNetworkSpawn()
    {
        StartCoroutine(ErrorMessage("Joined Lobby!"));

        ToggleEnterLobbyInterface(false);
        connectCanvas.SetActive(false);
    }

    public void ExitGame()
    {
        LeaveLobby();

        Application.Quit();
    }

    private async void LeaveLobby()
    {
        try
        {
            if (currentLobby != null)
            {
                if (IsServer)
                    await Lobbies.Instance.DeleteLobbyAsync(currentLobby.Id);
                else
                    await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            }
            
            // Avoids heartbeat errors in editor since playmode doesn't stop
            currentLobby = null;

            NetworkManager.Singleton.Shutdown();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    //.temp connect code for testing
    private bool alreadyConnected;
    private void Update()
    {
        if (alreadyConnected)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NetworkManager.Singleton.StartHost();
            alreadyConnected = true;
            connectCanvas.SetActive(false);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            NetworkManager.Singleton.StartClient();
            alreadyConnected = true;
            connectCanvas.SetActive(false);
        }
    }
}
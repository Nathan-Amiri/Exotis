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
    //.I NEED A WAY TO TRIGGER AUTOMATIC DRAWS

    //.FIX SHADOW TEXT COLOR HARD TO SEE

    //.HIGHLIGHT AVAILABLE ACTIONS FOR ALLY AND ENEMY, INCLUDING PASS BUTTON

    //.REMOVE SPELLGRAYS IN SCENE IF I DECIDE TO KEEP THEM ALL VISIBLE

    //.SHORTCUT MENU, DEFAULTS HAVE SPACE AS PASS SUBMIT AND CONSOLE BUTTON, X AS CANCEL, ENTER AS CHAT

    //.HOVER OVER UI IN SCENE FOR DESCRIPTIONS

    //.DON'T LOG CONSOLE MESSAGES IN CHAT

    //.HEALTH BAR COLOR CODED WITH SEGMENTS AND NO NUMBERS (MIGHT NEED ANOTHER INDICATOR FOR SPEED BESIDES COLOR?)

    //.A BACKGROUND FOR EACH ELEMENT, PREFERABLY AT LEAST SOMEWHAT ANIMATED

    //.TEXT WILL SAY "CASTER WILL RECAST X"

    //.SUBTLE VISUAL INDICATOR FOR ACTIONS

    //.Tutorial sections: Basics (including Retreat), Rounds/Counter/Recast, Spells, Elementals, Items/Status/Weariness
    //.Screens: Main Menu, game scene, Teambuilder, Wiki, Settings (game scene has an additional settings menu that's more limited)
    //.The only thing in the wiki not covered by the tutorial is priority (retreat>gem>spark>trait>spell)
    //.Never say Elementals are of type Element. Never use the word type, ever
    //.Can filter Elementals by element pools. They have the element colors in
    //.their icons of course, though this is never mentioned in game.Spells are
    //.sorted by Element themes, and this is talked about in the tutorial, but
    //.the only relation between elements and Elementals is the "spell pools."

    //.Can't use a Spell or Trait that involves Swapping if Trapped or no Swap available. The
    //.descriptions about this are fine, just find a way to make it clear in the tutorial.If I
    //.think of another better way to clarify this, great.But I can't do the Singe message,
    //.since TestForAvailableActions needs to be able to check it.
    


    [SerializeField] private GameObject connectCanvas;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;

    [SerializeField] private TMP_InputField usernameField;
    //[SerializeField] private TMP_InputField lobbyCodeField;

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
    //public void OnLobbyCodeChange()
    //{
    //    PlayerPrefs.SetString("LobbyCode", lobbyCodeField.text);
    //}

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
        //lobbyCodeField.interactable = on;
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
                //StartCoroutine(ErrorMessage("A lobby with that code already exists. Please choose another code"));
                StartCoroutine(ErrorMessage("The old game lobby is still being deleted. Try again in a few seconds"));
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
}
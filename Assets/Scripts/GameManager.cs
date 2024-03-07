using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

    //.Immediates are: Singe (when prompting the enemy to Swap), Leviathan (not counterable), Wraith, Thunderbird, Abomination. So no immediates are counterable

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
    

    //.temp code for faster connection
        bool alreadyConnected;
    private void Update()
    {
        if (alreadyConnected)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NetworkManager.Singleton.StartHost();
            alreadyConnected = true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            NetworkManager.Singleton.StartClient();
            alreadyConnected = true;
        }
    }
}
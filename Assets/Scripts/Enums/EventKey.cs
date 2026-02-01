// Enum for names/identifiers of events
public enum EventKey
{
    TEST_EVENT,

    // sound events
    SFX,
    MUSIC,
    STOP_MUSIC,
    PAUSE_MUSIC,
    MUTEMUSIC_TOGGLE,
    SFX_VOLUME_CHANGED,
    MUSIC_VOLUME_CHANGED,

    // Inputs
    MASK_INPUT,

    MASK_PICKUP,

    // scene system manager
    OPEN_SCENE,
    QUIT_GAME,
    LOADING_COMPLETE,

    // mask switching
    MASK_MODE_CHANGED,
    
    // Interaction
    INTERACTABLE_HIGHLIGHTED,
    INTERACTABLE_UNHIGHLIGHTED,
    INTERACTABLE_INTERACTED
}

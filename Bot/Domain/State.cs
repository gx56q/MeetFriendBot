namespace Bot.Domain;

public enum State
{
    Start,
    CreatingEvent,
    EditingEvent,
    EditingName,
    EditingDescription,
    EditingLocation,
    EditingDate,
    EditingPicture,
    EditingParticipants,
    EditingList,
    EditingListName,
    EditingListParticipants
}
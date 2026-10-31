using System.Collections.Concurrent;

namespace TypePulse;

public class PresenceStore
{
    /* Joins and disconnects can happen concurrently
    /  Several users can join or leave at the same time. 
    /  The field is private so other classes cannot change the data without using the store’s methods.
    */
    private readonly ConcurrentDictionary<string, string> _usersByConnection = new();


    /*
    / The dictionary indexer adds a new connection or updates the display name for an existing connection.
    / Values.ToArray() returns a snapshot of the current display names for the hub to broadcast.
    */
    public string[] AddOrUpdateUser(string connectionId, string displayName)
    {
        _usersByConnection[connectionId] = displayName;

        return _usersByConnection.Values.ToArray();
    }

    public string[] RemoveUser(string connectionId)
    {
        _usersByConnection.TryRemove(connectionId, out _);

        return _usersByConnection.Values.ToArray();
    }
}
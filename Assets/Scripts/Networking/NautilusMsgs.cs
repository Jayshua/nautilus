using UnityEngine.Networking;

public class MsgTypes
{
	// Client -> Server: Notifies the server of the prefab that the client selected
	public const short SelectClass = MsgType.Highest + 1;

	public class SelectClassMsg : MessageBase
	{
		public ClassType prefabIndex;

		public SelectClassMsg ()
		{
			prefabIndex = ClassType.LargeShip;
		}

		public SelectClassMsg (ClassType classType)
		{
			this.prefabIndex = classType;
		}
	}

	// Client -> Server: When the user selects the name they would like.
	// The client then waits for the IsNameOk message from the server.
	// Type: StringMessage
	public const short SelectName = MsgType.Highest + 2;

	// Server -> Client: When the server recieves the SelectName message
	// from the client, it responds with this message telling them
	// whether the name was selected.
	// Type: Boolean Message
	public const short IsNameOk = MsgType.Highest + 3;


	// A generic boolean message to compliment the StringMessage class
	// provided by Unity
	public class BooleanMessage : MessageBase
	{
		public bool value;

		public BooleanMessage ()
		{
			value = false;
		}

		public BooleanMessage (bool value)
		{
			this.value = value;
		}
	}
}
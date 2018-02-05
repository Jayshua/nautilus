using UnityEngine.Networking;

public class MsgTypes
{
	public const short PlayerPrefabSelect = MsgType.Highest + 1;
	public const short SelectName = MsgType.Highest + 2;
	public const short IsNameOk = MsgType.Highest + 3;

	public class BooleanMessage : MessageBase
	{
		public bool value;

		public BooleanMessage() {value = false;}

		public BooleanMessage (bool value)
		{
			this.value = value;
		}
	}

	public class PlayerPrefabMsg : MessageBase
	{
		public ClassType prefabIndex;
	}
}
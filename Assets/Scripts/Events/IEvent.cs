using System;

interface IEvent {
	void BeginEvent(NautilusNetworkManager gameController);
	event Action OnEnd;
}

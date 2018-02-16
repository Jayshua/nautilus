using System;

interface IEvent {
	void BeginEvent(NautilusServer gameController);
	event Action OnEnd;
}

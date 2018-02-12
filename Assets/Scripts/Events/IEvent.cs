using System;

interface IEvent {
	void Initialize(NautilusServer gameController);
	event Action OnEnd;
}

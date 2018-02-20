using System;

public interface IEvent {
	void BeginEvent(GameController gameController);
	event Action OnEnd;
}

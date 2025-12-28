from typing import List, Callable, Type, TypeVar, Any, Dict
from collections import defaultdict
from src.domain.interfaces import IEventBus
from src.domain.events import DomainEvent

T = TypeVar("T", bound=DomainEvent)


class InProcessEventBus(IEventBus):
    """
    A simple, synchronous in-memory event bus. 
    Perfect for decoupled communication within a single process. 🌿🏎️💨
    """

    def __init__(self):
        self._handlers: Dict[Type[DomainEvent], List[Callable[[Any], Any]]] = defaultdict(list)

    def publish(self, event: DomainEvent):
        """Dispatches the event to all registered handlers for its type. ⚡️"""
        event_type = type(event)
        for handler in self._handlers[event_type]:
            handler(event)

    def subscribe(self, event_type: Type[T], handler: Callable[[T], Any]):
        """Registers a handler for a specific event type. ⚓️"""
        self._handlers[event_type].append(handler)

import pytest
from src.infrastructure.event_handlers import LoggingEventHandler


def test_logging_handler_duration_formatting_all_branches(mocker):
    """
    Behavioral Test: Hits all duration formatting branches (ms, s, m, h)
    to ensure 100% logic coverage in the event handler! ⏱️📉✅
    """
    # Arrange
    handler = LoggingEventHandler(mocker.Mock(), mocker.Mock())
    
    # 1. Microseconds/Milliseconds only
    assert handler._format_duration(0.5) == "500ms"
    
    # 2. Seconds only
    assert handler._format_duration(1.5) == "1.500s"
    
    # 3. Minutes and Seconds
    assert handler._format_duration(65) == "1m 5s"
    
    # 4. Hours, Minutes, and Seconds
    assert handler._format_duration(3665) == "1h 1m 5s"

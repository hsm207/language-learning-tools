import pytest
from datetime import timedelta
from src.domain.value_objects import TimestampRange

def test_timestamp_range_valid():
    start = timedelta(seconds=10)
    end = timedelta(seconds=20)
    tr = TimestampRange(start, end)
    assert tr.start == start
    assert tr.end == end

def test_timestamp_range_invalid_raises_error():
    start = timedelta(seconds=20)
    end = timedelta(seconds=10)
    with pytest.raises(ValueError, match="Start time cannot be after end time"):
        TimestampRange(start, end)
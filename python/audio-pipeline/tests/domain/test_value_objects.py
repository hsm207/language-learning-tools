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



from src.domain.value_objects import Utterance, Word, ConfidenceScore



def test_utterance_prevents_words_outside_range():

    # Arrange

    u_range = TimestampRange(timedelta(seconds=1), timedelta(seconds=5))

    bad_word = Word("oops", TimestampRange(timedelta(seconds=0), timedelta(seconds=2)), ConfidenceScore(1.0))

    

    # Act & Assert

    with pytest.raises(ValueError, match="falls outside utterance range"):

        Utterance(

            timestamp=u_range,

            text="oops",

            speaker_id="S1",

            confidence=ConfidenceScore(1.0),

            words=[bad_word]

        )



def test_utterance_allows_words_within_range():

    # Arrange

    u_range = TimestampRange(timedelta(seconds=0), timedelta(seconds=5))

    good_word = Word("hello", TimestampRange(timedelta(seconds=1), timedelta(seconds=2)), ConfidenceScore(1.0))

    

    # Act

    u = Utterance(

        timestamp=u_range,

        text="hello",

        speaker_id="S1",

        confidence=ConfidenceScore(1.0),

        words=[good_word]

    )

    

    # Assert

    assert u.words[0] == good_word

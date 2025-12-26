import pytest
from datetime import timedelta
from src.infrastructure.diarization import PyannoteDiarizer
from src.domain.entities import AudioArtifact

def test_pyannote_diarizer_initialization_fails_without_token(mocker):
    # Use mocker to clear the environment! 🤫
    mocker.patch.dict("os.environ", {}, clear=True)
    
    with pytest.raises(ValueError, match="Missing HF_TOKEN"):
        PyannoteDiarizer()

def test_pyannote_diarizer_parses_output_correctly(mocker):
    # 1. Setup Mocks using the sexy mocker fixture 💉✨
    mocker.patch.dict("os.environ", {"HF_TOKEN": "hf_test_token"})
    
    # Mock the Pipeline class and its return values
    mock_pipeline_instance = mocker.Mock()
    mock_from_pretrained = mocker.patch("src.infrastructure.diarization.Pipeline.from_pretrained", return_value=mock_pipeline_instance)
    
    # Mock the complex DiarizeOutput structure 🕵️‍♀️
    mock_turn = mocker.Mock()
    mock_turn.start = 1.5
    mock_turn.end = 10.2
    
    mock_diarization = mocker.Mock()
    # itertracks(yield_label=True) returns (segment, track, label)
    mock_diarization.itertracks.return_value = [(mock_turn, "ignored_track", "SPEAKER_00")]
    
    mock_output = mocker.Mock()
    mock_output.speaker_diarization = mock_diarization
    mock_pipeline_instance.return_value = mock_output
    
    # 2. Execute 🚀
    diarizer = PyannoteDiarizer()
    artifact = AudioArtifact(file_path="test.wav")
    results = diarizer.diarize(artifact)
    
    # 3. Assert 💎
    assert len(results) == 1
    assert results[0].speaker_id == "SPEAKER_00"
    assert results[0].timestamp.start == timedelta(seconds=1.5)
    assert results[0].timestamp.end == timedelta(seconds=10.2)
    mock_from_pretrained.assert_called_once()

from src.domain.entities import ProcessingJob


def test_processing_job_utterances_fallback():
    """Hits the utterances property when no result/transcript is present. 🛡️⚖️"""
    job = ProcessingJob()
    assert job.utterances == []

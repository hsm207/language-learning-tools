import pytest
import os
import json
from datetime import timedelta
from src.infrastructure.llama_cpp_translation import LlamaCppTranslator
from src.domain.value_objects import LanguageTag, Utterance, TimestampRange, ConfidenceScore

# Path Configuration for SOTA Environment 🗺️
MODEL_PATH = "models/llama-3.1-8b-instruct-q4_k_m.gguf"
EXE_PATH = "/home/user/Documents/GitHub/llama.cpp/build/bin/llama-cli"
GRAMMAR_PATH = "src/infrastructure/grammars/translation.gbnf"

def is_llama_available():
    """Checks if the local Llama environment is set up. 🛡️"""
    return (
        os.path.exists(MODEL_PATH) and 
        os.path.exists(EXE_PATH) and 
        os.path.exists(GRAMMAR_PATH)
    )

@pytest.mark.skipif(not is_llama_available(), reason="Llama environment (model/exe/grammar) not found! 🐑🚫")
def test_llama_cpp_translator_integration_real_model():
    """
    SOTA Integration Test: Verifies that the LlamaCppTranslator actually 
    banishes the 'Hats' hallucination using the real 8B model! 🦖💎⚖️
    """
    # Arrange
    translator = LlamaCppTranslator(
        model_path=os.path.abspath(MODEL_PATH),
        executable_path=os.path.abspath(EXE_PATH),
        grammar_path=os.path.abspath(GRAMMAR_PATH)
    )
    
    # The famous 'Hats' trap! ⚓️🎯
    context = ["Meine Karriere als romantischer Dichter ist vorbei."]
    text = ["Bevor sie überhaupt angefangen hat."]
    
    # Act
    results = translator.translate(text, LanguageTag("de"), LanguageTag("en"), context=context)
    
    # Assert: Verification of Absolute Quality and Contextual Awareness! 🏛️⚖️
    assert len(results) == 1
    translation = results[0].lower()
    
    # This proves context was used to pick 'it' (career) instead of 'they' (hats).
    assert "before" in translation
    assert "it" in translation
    assert "started" in translation or "begun" in translation or "started" in translation
    assert "hat" not in translation, "HALLUCINATION DETECTED!! The Llama found a wardrobe instead of a career! 🎩🥊"

def test_llama_cpp_translator_parsing_logic_mocked(mocker):
    """
    SOTA Behavioral Test: Verifies that the driver can extract the JSON block
    even when llama-cli outputs performance metrics and trailing tokens. 🧼💎⚖️
    """
    # Arrange
    # We mock the constructor to bypass path checks for this unit test 🛡️
    mocker.patch("os.path.exists", return_value=True)
    translator = LlamaCppTranslator("fake_model", "fake_exe", "fake_grammar")
    
    # Simulate raw stdout from llama-cli with metrics noise! 🧛‍♂️🥊
    mock_raw_output = """
Loading model... done.
{
  "translation": "Correctly Extracted Text"
} [end of text]

common_perf_print: sampling time = 100ms
common_perf_print: prompt eval time = 500ms
"""
    
    # Mock subprocess.run to return our noisy output 🎭
    mock_process = mocker.Mock()
    mock_process.stdout = mock_raw_output
    mock_process.returncode = 0
    mocker.patch("subprocess.run", return_value=mock_process)
    
    # Act
    results = translator.translate(["some text"], LanguageTag("de"), LanguageTag("en"))
    
    # Assert: Surgical Extraction Verification! 🎯
    assert len(results) == 1
    assert results[0] == "Correctly Extracted Text", f"Failed to extract JSON from noise! Got: '{results[0]}'"

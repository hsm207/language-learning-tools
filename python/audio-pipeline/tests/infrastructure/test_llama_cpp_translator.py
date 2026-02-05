import pytest
import os
import subprocess
from src.infrastructure.llama_cpp_translation import LlamaCppTranslator
from src.domain.value_objects import LanguageTag

# Path Configuration for Environment 🗺️
MODEL_PATH = "models/llama-3.1-8b-instruct-q4_k_m.gguf"
EXE_PATH = "/home/user/Documents/GitHub/llama.cpp/build/bin/llama-cli"
GRAMMAR_PATH = "src/infrastructure/grammars/translation.gbnf"


def is_llama_available():
    """Checks if the local Llama environment is set up. 🛡️"""
    return (
        os.path.exists(MODEL_PATH)
        and os.path.exists(EXE_PATH)
        and os.path.exists(GRAMMAR_PATH)
    )


@pytest.mark.skipif(
    not is_llama_available(),
    reason="Llama environment (model/exe/grammar) not found! 🐑🚫",
)
def test_llama_cpp_translator_integration_real_model():
    """
    Integration Test: Verifies that the LlamaCppTranslator actually
    banishes the 'Hats' hallucination using the real 8B model! 🦖💎⚖️
    """
    # Arrange
    translator = LlamaCppTranslator(
        model_path=os.path.abspath(MODEL_PATH),
        executable_path=os.path.abspath(EXE_PATH),
        grammar_path=os.path.abspath(GRAMMAR_PATH),
    )

    # The famous 'Hats' trap! ⚓️🎯
    context = ["Meine Karriere als romantischer Dichter ist vorbei."]
    text = ["Bevor sie überhaupt angefangen hat."]

    # Act
    results = translator.translate(
        text, LanguageTag("de"), LanguageTag("en"), context=context
    )

    # Assert: Verification of Absolute Quality and Contextual Awareness! 🏛️⚖️
    assert len(results) == 1
    translation = results[0].lower()

    # This proves context was used to pick 'it' (career) instead of 'they' (hats).
    assert "before" in translation
    assert "it" in translation
    assert (
        "started" in translation or "begun" in translation or "started" in translation
    )
    assert (
        "hat" not in translation
    ), "HALLUCINATION DETECTED!! The Llama found a wardrobe instead of a career! 🎩🥊"


def test_llama_cpp_translator_parsing_logic_mocked(mocker):
    """
    Behavioral Test: Verifies that the driver can extract the JSON block
    even when llama-cli outputs performance metrics and trailing tokens. 🧼💎⚖️
    """
    # Arrange
    mocker.patch("os.path.exists", return_value=True)
    translator = LlamaCppTranslator("fake_model", "fake_exe", "fake_grammar")

    mock_raw_output = b"""
Loading model... done.
{
  "translations": ["Correctly Extracted Text 1", "Second Translation"]
}
[end of text]

common_perf_print: sampling time = 100ms
"""

    mock_process = mocker.Mock()
    mock_process.stdout = mock_raw_output
    mock_process.returncode = 0
    mocker.patch("subprocess.run", return_value=mock_process)

    # Act
    results = translator.translate(["text 1", "text 2"], LanguageTag("de"), LanguageTag("en"))

    # Assert: Surgical Extraction Verification! 🎯
    assert results[0] == "Correctly Extracted Text 1"
    assert results[1] == "Second Translation"



def test_llama_cpp_translator_handles_empty_input(mocker):
    """Verifies that empty input lists are handled gracefully. 🌬️🗑️"""
    mocker.patch("os.path.exists", return_value=True)
    translator = LlamaCppTranslator("m", "e", "g")
    assert translator.translate([], LanguageTag("de"), LanguageTag("en")) == []


def test_llama_cpp_translator_handles_subprocess_error(mocker):
    """Verifies that subprocess crashes don't bring down the pipeline. 🦖🥊"""
    mocker.patch("os.path.exists", return_value=True)
    mocker.patch("subprocess.run", side_effect=subprocess.CalledProcessError(1, "cmd"))
    translator = LlamaCppTranslator("m", "e", "g")

    results = translator.translate(["text"], LanguageTag("de"), LanguageTag("en"))
    assert results == [""]


def test_llama_cpp_translator_handles_invalid_json(mocker):
    """Verifies that malformed model output is handled safely. 🧬🥊"""
    mocker.patch("os.path.exists", return_value=True)
    mock_res = mocker.Mock(stdout=b"{ 'broken': 'json' }")
    mocker.patch("subprocess.run", return_value=mock_res)
    translator = LlamaCppTranslator("m", "e", "g")

    results = translator.translate(["text"], LanguageTag("de"), LanguageTag("en"))
    assert results == [""]


def test_llama_cpp_translator_handles_no_json_found(mocker):
    """Verifies behavior when the output contains no braces. 🚫📦"""
    mocker.patch("os.path.exists", return_value=True)
    mock_res = mocker.Mock(stdout=b"No JSON here, just chatty vibes!")
    mocker.patch("subprocess.run", return_value=mock_res)
    translator = LlamaCppTranslator("m", "e", "g")

    results = translator.translate(["text"], LanguageTag("de"), LanguageTag("en"))
    assert results == [""]


def test_llama_cpp_translator_verify_dependencies_fails(mocker):
    """Verifies initialization fails when files are missing. 🚫🔨"""
    mocker.patch("os.path.exists", return_value=False)
    with pytest.raises(FileNotFoundError, match="Required dependency not found"):
        LlamaCppTranslator("m", "e", "g")

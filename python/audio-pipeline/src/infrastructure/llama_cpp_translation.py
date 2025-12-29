import json
import subprocess
import os
import dataclasses
from typing import List, Optional
from src.domain.interfaces import ITranslator, ILogger
from src.infrastructure.logging import NullLogger
from src.domain.value_objects import LanguageTag


class LlamaCppTranslator(ITranslator):
    """
    Inference driver for llama.cpp using GBNF grammars to ensure structured JSON output. 🦖⛓️💎
    """

    # Llama 3.1 Instruct Template Constants 🏛️
    SYSTEM_PREFIX = "<|start_header_id|>system<|end_header_id|>\n\n"
    USER_PREFIX = "<|start_header_id|>user<|end_header_id|>\n\n"
    ASSISTANT_PREFIX = "<|start_header_id|>assistant<|end_header_id|>\n\n"
    EOT = "<|eot_id|>"

    def __init__(
        self,
        model_path: str,
        executable_path: str,
        grammar_path: str,
        n_ctx: int = 2048,
        threads: int = None,
        logger: ILogger = NullLogger(),
    ):
        self.model_path = model_path
        self.executable_path = executable_path
        self.grammar_path = grammar_path
        self.n_ctx = n_ctx
        self.threads = threads or (os.cpu_count() // 2)
        self.logger = logger

        self._verify_dependencies()

    def translate(
        self,
        texts: List[str],
        source_lang: LanguageTag,
        target_lang: LanguageTag,
        context: List[str] = None,
    ) -> List[str]:
        if not texts:
            return []

        return [self._translate_single(text, context) for text in texts]

    def _translate_single(self, text: str, context: Optional[List[str]]) -> str:
        """Orchestrates the translation of a single text turn. ⚓️🎯"""
        prompt = self._build_prompt(text, context)

        try:
            raw_output = self._run_inference(prompt)
            return self._extract_field(raw_output, "translation")
        except Exception as e:
            self.logger.error(f"❌ Local Llama translation failed: {str(e)}")
            return ""

    def _run_inference(self, prompt: str) -> str:
        """Executes the llama-cli process and captures the raw output. 🏎️💨"""
        cmd = [
            self.executable_path,
            "-m",
            self.model_path,
            "-p",
            prompt,
            "--grammar-file",
            self.grammar_path,
            "-n",
            "128",  # Predict max 128 tokens
            "--temp",
            "0.1",  # Low temperature for deterministic output
            "--threads",
            str(self.threads),
            "--ctx-size",
            str(self.n_ctx),
            "--no-display-prompt",  # Don't echo the prompt to stdout
            "--log-disable",  # Suppress llama.cpp banner/metrics
            "-st",  # Single-turn mode (exit after EOT)
            "--simple-io",  # Minimalist IO for cleaner stream capture
        ]

        # Internal Technical Log! 🕵️‍♀️🔬
        self.logger.debug(f"🚀 Spawning Llama-CLI for local inference...")
        # Capture as bytes to avoid UTF-8 decoding crashes on weird LLM artifacts! 🧼🛡️
        process = subprocess.run(cmd, capture_output=True, text=False, check=True)
        return process.stdout.decode("utf-8", errors="replace").strip()

    def _extract_field(self, raw_output: str, field_name: str) -> str:
        """Surgically extracts a field from the first JSON block found in output. ✂️💎"""
        # We slice between '{' and '}' because llama-cli often appends trailing artifacts
        # like ' [end of text]', metrics, or newlines that break direct json.loads() calls.
        json_start = raw_output.find("{")
        json_end = raw_output.rfind("}")

        if json_start == -1 or json_end == -1:
            self.logger.error(f"❌ No JSON block found in Llama output: {raw_output}")
            return ""

        json_str = raw_output[json_start : json_end + 1]
        try:
            data = json.loads(json_str)
            return data.get(field_name, "").strip()
        except json.JSONDecodeError:
            self.logger.error(f"❌ Failed to parse extracted JSON: {json_str}")
            return ""

    def _build_prompt(self, text: str, context: List[str] = None) -> str:
        """Constructs a high-fidelity Llama 3.1 Instruct prompt with laser focus. 🏛️💎"""
        system_msg = (
            "You are a specialized translation engine. Your ONLY task is to translate the string labeled 'TARGET'. "
            "The 'CONTEXT' strings are for reference only—DO NOT translate them. "
            "Output ONLY the English translation of the 'TARGET' string in the required JSON format."
        )

        context_str = "\n".join(context) if context else "None"
        user_msg = (
            f"CONTEXT (for reference only):\n{context_str}\n\n"
            f"TARGET (translate this line):\n{text}"
        )

        return (
            f"{self.SYSTEM_PREFIX}{system_msg}{self.EOT}"
            f"{self.USER_PREFIX}{user_msg}{self.EOT}"
            f"{self.ASSISTANT_PREFIX}"
        )

    def _verify_dependencies(self):
        """Ensures all required paths exist on disk. 🕵️‍♀️🔬"""
        for p in [self.model_path, self.executable_path, self.grammar_path]:
            if not os.path.exists(p):
                raise FileNotFoundError(f"Required dependency not found: {p}")

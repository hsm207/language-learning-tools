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

        prompt = self._build_prompt(texts, context)

        try:
            raw_output = self._run_inference(prompt, len(texts))
            translations = self._extract_list_field(raw_output, "translations")
            
            if len(translations) != len(texts):
                self.logger.warning(f"⚠️ Translation count mismatch! Expected {len(texts)}, got {len(translations)}")
                # Fill missing with empty strings or slice if too many
                return (translations + [""] * len(texts))[:len(texts)]
            
            return translations
        except Exception as e:
            self.logger.error(f"❌ Local Llama batch translation failed: {str(e)}")
            return [""] * len(texts)

    def _run_inference(self, prompt: str, batch_size: int = 1) -> str:
        """Executes the llama-cli process and captures the raw output. 🏎️💨"""
        # Dynamic token limit: 128 per text turn 📈💎
        max_tokens = 128 * batch_size
        
        cmd = [
            self.executable_path,
            "-m",
            self.model_path,
            "-p",
            prompt,
            "--grammar-file",
            self.grammar_path,
            "-n",
            str(max_tokens),
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

    def _extract_list_field(self, raw_output: str, field_name: str) -> List[str]:
        """Extracts a list field from the first JSON block found in output. ✂️💎"""
        json_start = raw_output.find("{")
        json_end = raw_output.rfind("}")

        if json_start == -1 or json_end == -1:
            self.logger.error(f"❌ No JSON block found in Llama output: {raw_output}")
            return []

        json_str = raw_output[json_start : json_end + 1]
        try:
            data = json.loads(json_str)
            result = data.get(field_name, [])
            return [str(s).strip() for s in result]
        except json.JSONDecodeError:
            self.logger.error(f"❌ Failed to parse extracted JSON: {json_str}")
            return []

    def _build_prompt(self, texts: List[str], context: List[str] = None) -> str:
        """Constructs a high-fidelity Llama 3.1 Instruct prompt for batch translation. 🏛️💎"""
        system_msg = (
            "You are a specialized translation engine. Your task is to translate the list of strings labeled 'TARGETS'. "
            "The 'CONTEXT' strings are for reference only—DO NOT translate them. "
            "Output a JSON object with a single key 'translations' containing an array of English translations "
            "corresponding 1:1 to the input strings."
        )

        context_str = "\n".join(context) if context else "None"
        targets_str = "\n".join([f"{i+1}. {t}" for i, t in enumerate(texts)])
        
        user_msg = (
            f"CONTEXT (for reference only):\n{context_str}\n\n"
            f"TARGETS (translate these {len(texts)} lines individually):\n{targets_str}"
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

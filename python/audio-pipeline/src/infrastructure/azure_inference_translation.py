import json
import re
import time
from typing import Any, Dict, List, Optional

import httpx

from src.domain.interfaces import ILogger, ITranslator
from src.domain.value_objects import LanguageTag
from src.infrastructure.logging import NullLogger


class AzureInferenceTranslator(ITranslator):
    """
    Adapter for the Azure AI Inference API to perform high-fidelity translations.

    This implementation uses structured JSON outputs to ensure data alignment and
    incorporates exponential backoff to handle rate limits (HTTP 429).
    """

    def __init__(
        self,
        endpoint: str,
        api_key: str,
        logger: ILogger = NullLogger(),
    ):
        self.endpoint = endpoint
        self.api_key = api_key
        self.logger = logger
        self.model_name = self._extract_model_name(endpoint)

    def translate(
        self,
        texts: List[str],
        source_lang: LanguageTag,
        target_lang: LanguageTag,
        context: Optional[List[str]] = None,
    ) -> List[str]:
        """
        Translates a list of texts using context-aware batch processing.
        """
        if not texts:
            return []

        payload = self._prepare_payload(texts, source_lang, target_lang, context)
        headers = {
            "Content-Type": "application/json",
            "api-key": self.api_key,
        }

        return self._execute_with_retries(texts, payload, headers)

    def _extract_model_name(self, endpoint: str) -> str:
        """Extracts the deployment model name from the Azure endpoint URL."""
        match = re.search(r"/deployments/([^/?]+)", endpoint)
        return match.group(1) if match else "model"

    def _prepare_payload(
        self,
        texts: List[str],
        source_lang: LanguageTag,
        target_lang: LanguageTag,
        context: Optional[List[str]],
    ) -> Dict[str, Any]:
        """Constructs the JSON payload for the Azure AI Inference request."""
        items = [{"id": str(i), "text": t} for i, t in enumerate(texts)]

        system_msg = (
            f"You are a professional translator. Your task is to translate a list of {source_lang} strings into {target_lang}. "
            "You MUST output a JSON object containing an array of translations. "
            "Each translation must have the SAME numeric ID as the input. "
            "The provided 'context_reference' is for disambiguation only—DO NOT translate it."
        )

        user_content = {
            "context_reference": "\n".join(context) if context else "None",
            "items_to_translate": items,
        }

        return {
            "messages": [
                {"role": "system", "content": system_msg},
                {"role": "user", "content": json.dumps(user_content)},
            ],
            "model": self.model_name,
            "temperature": 0.1,
            "response_format": {
                "type": "json_schema",
                "json_schema": self._get_response_schema(),
            },
            "max_tokens": 4096,
            "stream": False,
        }

    def _get_response_schema(self) -> Dict[str, Any]:
        """Defines the structured output schema for the translation response."""
        return {
            "name": "translation_response",
            "strict": True,
            "schema": {
                "type": "object",
                "properties": {
                    "translations": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "id": {"type": "string"},
                                "text": {"type": "string"},
                            },
                            "required": ["id", "text"],
                            "additionalProperties": False,
                        },
                    }
                },
                "required": ["translations"],
                "additionalProperties": False,
            },
        }

    def _execute_with_retries(
        self,
        texts: List[str],
        payload: Dict[str, Any],
        headers: Dict[str, Any],
    ) -> List[str]:
        """Executes the HTTP request with retry logic and exponential backoff."""
        max_retries = 3
        base_delay = 65.0

        for attempt in range(max_retries):
            try:
                if attempt > 0:
                    self.logger.warning(
                        f"Retry attempt {attempt + 1} for Azure Inference translation."
                    )

                with httpx.Client(timeout=120.0) as client:
                    response = client.post(self.endpoint, headers=headers, json=payload)

                    if response.status_code == 429:
                        self._handle_backoff(attempt, base_delay)
                        continue

                    response.raise_for_status()
                    return self._parse_response(texts, response.json())

            except Exception as e:
                if attempt == max_retries - 1:
                    self.logger.error(
                        f"Azure Inference translation failed after {max_retries} attempts: {e}"
                    )
                    break
                self._handle_backoff(attempt, base_delay)

        return [""] * len(texts)

    def _handle_backoff(self, attempt: int, base_delay: float) -> None:
        """Calculates and executes backoff delay."""
        wait_time = base_delay * (2**attempt)
        self.logger.warning(f"Rate limited (429). Backing off for {wait_time}s...")
        time.sleep(wait_time)

    def _parse_response(self, texts: List[str], data: Dict[str, Any]) -> List[str]:
        """Parses the API response and maps translations back to original order."""
        if not data.get("choices"):
            self.logger.error("Azure Inference returned an empty choices list.")
            return [""] * len(texts)

        content_str = data["choices"][0]["message"]["content"]
        self.logger.debug(f"Raw model output: {content_str}")

        result_data = json.loads(content_str)
        items = result_data.get("translations", [])

        results_map = {item["id"]: item["text"] for item in items}
        return [results_map.get(str(i), "").strip() for i in range(len(texts))]

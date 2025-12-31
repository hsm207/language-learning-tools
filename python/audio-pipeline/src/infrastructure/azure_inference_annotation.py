import json
import time
from typing import Any, Dict, List, Optional
import httpx

from src.domain.interfaces import ILogger, ILinguisticAnnotationService
from src.domain.value_objects import LanguageTag
from src.infrastructure.logging import NullLogger
from src.infrastructure.azure_inference_annotation_mapper import AzureInferenceAnnotationMapper

class AzureInferenceAnnotationService(ILinguisticAnnotationService):
    """
    Azure AI Inference implementation for linguistic annotation.
    Humble Object: Handles network concerns and retries. 📡🛡️✨
    """

    def __init__(
        self,
        endpoint: str,
        api_key: str,
        logger: ILogger = NullLogger(),
        mapper: Optional[AzureInferenceAnnotationMapper] = None
    ):
        self.endpoint = endpoint
        self.api_key = api_key
        self.logger = logger
        self.mapper = mapper or AzureInferenceAnnotationMapper()
        self.model_name = self.mapper.extract_model_name(endpoint)

    def annotate(
        self,
        texts: List[str],
        language: LanguageTag,
        context: Optional[List[str]] = None,
    ) -> List[Optional[str]]:
        if not texts:
            return []

        payload = self.mapper.prepare_payload(texts, language, self.model_name, context)
        headers = {
            "Content-Type": "application/json",
            "api-key": self.api_key,
        }

        return self._execute_with_retries(len(texts), payload, headers)

    def _execute_with_retries(
        self,
        num_texts: int,
        payload: Dict[str, Any],
        headers: Dict[str, Any],
    ) -> List[Optional[str]]:
        max_retries = 3
        base_delay = 65.0

        for attempt in range(max_retries):
            try:
                with httpx.Client(timeout=120.0) as client:
                    response = client.post(self.endpoint, headers=headers, json=payload)
                    if response.status_code == 429:
                        time.sleep(base_delay * (2**attempt))
                        continue
                    response.raise_for_status()
                    return self.mapper.parse_response(num_texts, response.json())
            except Exception as e:
                if attempt == max_retries - 1:
                    self.logger.error(f"Annotation failed: {e}")
                    break
                time.sleep(base_delay * (2**attempt))

        return [None] * num_texts

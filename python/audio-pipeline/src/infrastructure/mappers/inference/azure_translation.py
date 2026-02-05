import json
import re
from typing import Any, Dict, List, Optional
from src.domain.value_objects import LanguageTag

class AzureInferenceTranslationMapper:
    """
    Pure logic mapper for Azure AI Inference translation. 🌍💎
    Handles payload construction and response parsing without any side effects.
    """

    def extract_model_name(self, endpoint: str) -> str:
        """Extracts the deployment name from the Azure endpoint URL. 🕵️‍♀️🔬"""
        match = re.search(r"/deployments/([^/?]+)", endpoint)
        return match.group(1) if match else "model"

    def prepare_payload(
        self,
        texts: List[str],
        target_lang: LanguageTag,
        model_name: str,
        context: Optional[List[str]],
    ) -> Dict[str, Any]:
        """Constructs the JSON payload for the Azure Inference API. 📦✨"""
        items = [{"id": str(i), "text": t} for i, t in enumerate(texts)]

        system_msg = (
            f"You are a professional translator. Translate the provided list of text items into {target_lang}. "
            "IMPORTANT: Maintain the exact same number of items. "
            "Use the provided 'context_reference' to ensure consistency and correct terminology. "
            "Output MUST be a JSON object containing an array of translations."
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
            "model": model_name,
            "temperature": 0.3,
            "response_format": {
                "type": "json_schema",
                "json_schema": self.get_response_schema(),
            },
            "max_tokens": 4096,
            "stream": False,
        }

    def get_response_schema(self) -> Dict[str, Any]:
        """Defines the strict JSON schema for the AI response. 📏⚖️"""
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

    def parse_response(self, num_texts: int, data: Dict[str, Any]) -> List[str]:
        """Parses the raw API response back into a list of translated strings. 🧼🚿🎯"""
        content_str = data["choices"][0]["message"]["content"]
        result_data = json.loads(content_str)
        items = result_data.get("translations", [])

        results_map = {item["id"]: item["text"] for item in items}
        # Fallback to empty string if ID is missing to maintain list length
        return [results_map.get(str(i), "") for i in range(num_texts)]

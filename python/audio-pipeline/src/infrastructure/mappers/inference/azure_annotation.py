import json
import re
from typing import Any, Dict, List, Optional
from src.domain.value_objects import LanguageTag

class AzureInferenceAnnotationMapper:
    """
    Pure logic mapper for Azure AI Inference annotation. 👩‍🏫💎
    Handles payload construction and response parsing without any side effects.
    """

    def extract_model_name(self, endpoint: str) -> str:
        """Extracts the deployment name from the Azure endpoint URL. 🕵️‍♀️🔬"""
        match = re.search(r"/deployments/([^/?]+)", endpoint)
        return match.group(1) if match else "model"

    def prepare_payload(
        self,
        texts: List[str],
        language: LanguageTag,
        model_name: str,
        context: Optional[List[str]],
    ) -> Dict[str, Any]:
        """Constructs the JSON payload for the Azure Inference API. 📦✨"""
        items = [{"id": str(i), "text": t} for i, t in enumerate(texts)]

        system_msg = (
            f"You are a strict and highly detailed language tutor for {language} learners. "
            "Your mission is to find linguistic artifacts in speech segments. "
            "IMPORTANT: You are given a 'context_reference' containing both preceding and following segments. "
            "Use this bidirectional context to understand the grammatical flow. "
            "Continuations and Reported Speech (e.g., Konjunktiv I like 'er sei', 'sie seien') are CORRECT in context. "
            "Do NOT flag correct continuations as errors. "
            "You MUST provide a note for: "
            "1) GRAMMAR ERRORS: Like wrong conjugations (e.g., 'Ich vertritt' should be 'Ich vertrete'). "
            "2) SPEECH ARTIFACTS: Like repetitions ('der der'), filler words, or stutters. "
            "3) FRAGMENTS: Like bits of abbreviations ('C.', 'D.', 'U.'). Explain what the full word likely is. "
            "4) SLANG/REGIONALISMS: Explain terms that a learner might not find in a standard dictionary. "
            "Notes must be CONCISE English. "
            "CRITICAL: If a segment is 100% textbook-perfect or correctly follows its context, return exactly 'OK'. "
            "You MUST output a JSON object containing an array of annotations."
        )

        user_content = {
            "context_reference": "\n".join(context) if context else "None",
            "items_to_annotate": items,
        }

        return {
            "messages": [
                {"role": "system", "content": system_msg},
                {"role": "user", "content": json.dumps(user_content)},
            ],
            "model": model_name,
            "temperature": 0.1,
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
            "name": "annotation_response",
            "strict": True,
            "schema": {
                "type": "object",
                "properties": {
                    "annotations": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "id": {"type": "string"},
                                "note": {"type": "string"},
                            },
                            "required": ["id", "note"],
                            "additionalProperties": False,
                        },
                    }
                },
                "required": ["annotations"],
                "additionalProperties": False,
            },
        }

    def parse_response(self, num_texts: int, data: Dict[str, Any]) -> List[Optional[str]]:
        """Parses the raw API response back into a list of notes. 🧼🚿🎯"""
        content_str = data["choices"][0]["message"]["content"]
        result_data = json.loads(content_str)
        items = result_data.get("annotations", [])

        results_map = {}
        for item in items:
            note = str(item.get("note", "")).strip()
            # ⚓️ Map Sentinel back to None
            if note.upper() == "OK":
                results_map[item["id"]] = None
            else:
                results_map[item["id"]] = note

        return [results_map.get(str(i)) for i in range(num_texts)]

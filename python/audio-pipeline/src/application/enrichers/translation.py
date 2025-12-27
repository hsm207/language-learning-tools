import dataclasses
from typing import List
from src.domain.interfaces import IAudioEnricher, ITranslator, ILogger, NullLogger
from src.domain.value_objects import Utterance, LanguageTag


class TranslationEnricher(IAudioEnricher):
    """
    Translates utterances into a target language using a provided translator.
    Adheres to the Strategy pattern to support any SOTA translation model! 🌍💎⚖️
    """

    def __init__(
        self,
        translator: ITranslator,
        target_lang: LanguageTag,
        batch_size: int = 10,
        logger: ILogger = NullLogger(),
    ):
        self.translator = translator
        self.target_lang = target_lang
        self.batch_size = batch_size
        self.logger = logger

    def enrich(
        self, utterances: List[Utterance], language: LanguageTag
    ) -> List[Utterance]:
        if not utterances:
            return []

        self.logger.info(
            f"🌍 Translating {len(utterances)} utterances to {self.target_lang}..."
        )
        enriched = []

        # Batch translation to stay efficient and avoid overhead! 📦💨
        for i in range(0, len(utterances), self.batch_size):
            batch = utterances[i : i + self.batch_size]
            texts = [u.text for u in batch]

            try:
                translated_texts = self.translator.translate(
                    texts, source_lang=language, target_lang=self.target_lang
                )

                if len(translated_texts) != len(batch):
                    self.logger.warning(
                        f"Translation count mismatch! Expected {len(batch)}, got {len(translated_texts)}. Falling back to empty translations."
                    )
                    translated_texts = [""] * len(batch)

                for u, translated in zip(batch, translated_texts):
                    enriched.append(dataclasses.replace(u, translated_text=translated))

            except Exception as e:
                self.logger.error(f"Translation batch failed: {str(e)}")
                # Fallback: keep original but with empty translation
                for u in batch:
                    enriched.append(dataclasses.replace(u, translated_text=""))

        return enriched

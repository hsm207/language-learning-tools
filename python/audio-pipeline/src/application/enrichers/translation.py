import dataclasses
from typing import List
from src.domain.interfaces import IAudioEnricher, ITranslator, ILogger
from src.domain.value_objects import Utterance, LanguageTag


class TranslationEnricher(IAudioEnricher):
    """
    Orchestrates translation of utterances using an injected ITranslator implementation.
    Manages sliding window context for improved translation accuracy. 🌍💎⚖️
    """

    def __init__(
        self,
        translator: ITranslator,
        target_lang: LanguageTag,
        logger: ILogger,
        batch_size: int = 10,
        context_size: int = 0,
    ):
        self.translator = translator
        self.target_lang = target_lang
        self.batch_size = batch_size
        self.context_size = context_size
        self.logger = logger

    def enrich(
        self, utterances: List[Utterance], language: LanguageTag
    ) -> List[Utterance]:
        self.logger.info(
            f"🌍 Translating {len(utterances)} utterances to {self.target_lang} (context_size={self.context_size})..."
        )
        enriched = []

        for i in range(0, len(utterances), self.batch_size):
            target_batch = utterances[i : i + self.batch_size]

            context_start = max(0, i - self.context_size)
            context_batch = utterances[context_start:i]

            texts = [u.text for u in target_batch]
            context_texts = [u.text for u in context_batch]

            try:
                translated_texts = self.translator.translate(
                    texts,
                    source_lang=language,
                    target_lang=self.target_lang,
                    context=context_texts,
                )

                if len(translated_texts) != len(target_batch):
                    self.logger.warning(
                        f"⚠️ Translation count mismatch! Expected {len(target_batch)}, got {len(translated_texts)}."
                    )
                    translated_texts = [""] * len(target_batch)

                for u, translated in zip(target_batch, translated_texts):
                    enriched.append(dataclasses.replace(u, translated_text=translated))

            except Exception as e:
                self.logger.error(f"❌ Translation batch failed: {str(e)}")
                for u in target_batch:
                    enriched.append(dataclasses.replace(u, translated_text=""))

        return enriched

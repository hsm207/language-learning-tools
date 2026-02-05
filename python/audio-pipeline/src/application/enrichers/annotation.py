import dataclasses
from typing import List
from src.domain.interfaces import IAudioEnricher, ILinguisticAnnotationService, ILogger
from src.domain.value_objects import Utterance, LanguageTag

class LinguisticAnnotationEnricher(IAudioEnricher):
    """
    Orchestrates linguistic annotation for utterances to provide 
    pedagogical feedback to language learners. 🎓💎✨
    """

    def __init__(
        self,
        annotation_service: ILinguisticAnnotationService,
        logger: ILogger,
        batch_size: int = 1, # Strict 1:1 for now! 📏⚖️
        context_size: int = 10,
    ):
        self.annotation_service = annotation_service
        self.batch_size = batch_size
        self.context_size = context_size
        self.logger = logger

    def enrich(
        self, utterances: List[Utterance], language: LanguageTag
    ) -> List[Utterance]:
        self.logger.info(f"🎓 Annotating {len(utterances)} utterances for learners (context_size={self.context_size})...")
        
        enriched_utterances = list(utterances)
        
        for i in range(0, len(utterances), self.batch_size):
            batch_slice = utterances[i : i + self.batch_size]
            batch_texts = [u.text for u in batch_slice]
            
            # 📜 Panoramic Context Construction 🏔️
            pre_start = max(0, i - self.context_size)
            pre_context = [u.text for u in utterances[pre_start:i]]
            
            post_end = min(len(utterances), i + self.batch_size + self.context_size)
            post_context = [u.text for u in utterances[i + self.batch_size : post_end]]
            
            try:
                # Call the decoupled annotation service 📡✨
                annotations = self.annotation_service.annotate(
                    texts=batch_texts,
                    language=language,
                    context=pre_context + ["--- TARGET SEGMENT(S) BELOW ---"] + post_context
                )
                
                # 🛡️ Contract Validation: Ensure we got exactly what we asked for!
                if len(annotations) != len(batch_slice):
                    raise RuntimeError(f"Annotation count mismatch! Expected {len(batch_slice)}, got {len(annotations)}")

                # Map annotations back to utterances with surgical precision 🎯
                for j, annotation in enumerate(annotations):
                    new_metadata = dict(enriched_utterances[i + j].metadata)
                    new_metadata["learner_notes"] = annotation
                    enriched_utterances[i + j] = dataclasses.replace(
                        enriched_utterances[i + j],
                        metadata=new_metadata
                    )
                        
            except Exception as e:
                self.logger.error(f"❌ Annotation failed for batch starting at {i}: {e}")
                # 🚩 Resilience Sentinel: Mark the batch as 'Unverified'
                for j in range(len(batch_slice)):
                    new_metadata = dict(enriched_utterances[i + j].metadata)
                    new_metadata["learner_notes"] = "[Annotation Service Unavailable ⚠️]"
                    enriched_utterances[i + j] = dataclasses.replace(
                        enriched_utterances[i + j],
                        metadata=new_metadata
                    )

        return enriched_utterances

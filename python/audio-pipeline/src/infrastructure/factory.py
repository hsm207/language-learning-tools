from typing import List, Tuple
import os
from src.domain.interfaces import (
    ITranscriber,
    IDiarizer,
    IAudioProcessor,
    IAudioEnricher,
    ILogger,
    IAlignmentService,
    ITranslator,
)
from src.domain.value_objects import LanguageTag
from src.infrastructure.stack_builders import BUILDER_REGISTRY


class PipelineComponentFactory:
    """
    Composition Root Factory. 🏗️✨
    Encapsulates the construction logic for different pipeline stacks to remain OCP-compliant.
    """

    def __init__(self, args, logger: ILogger):
        self.args = args
        self.logger = logger

    def build_components(
        self,
    ) -> Tuple[
        IAudioProcessor,
        ITranscriber,
        IDiarizer,
        IAlignmentService,
        List[IAudioEnricher],
    ]:
        stack_key = "azure" if self.args.use_azure else "local"
        builder_class = BUILDER_REGISTRY.get(stack_key)

        if not builder_class:
            raise ValueError(f"❌ Unknown stack type: '{stack_key}'! 🛡️⚖️🏛️")

        builder = builder_class()
        return builder.build(self.args, self.logger)


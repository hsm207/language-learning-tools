import inspect
import pytest
from typing import Type, get_type_hints
from src.domain.interfaces import (
    IEventBus,
    IResultRepository,
    ITranscriptSerializer,
    ITranscriber,
    IDiarizer,
    IAudioProcessor,
    ITranslator,
    ILinguisticAnnotationService,
    ITelemetryService,
    ILogger,
    IAlignmentService,
    IAudioEnricher,
)
from src.infrastructure.bus import InProcessEventBus
from src.infrastructure.repositories import FileSystemResultRepository
from src.infrastructure.serialization import JsonTranscriptSerializer
from src.infrastructure.transcription import WhisperTranscriber, AzureFastTranscriber
from src.infrastructure.diarization import PyannoteDiarizer, NullDiarizer
from src.infrastructure.audio import FFmpegAudioProcessor
from src.infrastructure.azure_inference_translation import AzureInferenceTranslator
from src.infrastructure.llama_cpp_translation import LlamaCppTranslator
from src.infrastructure.azure_inference_annotation import AzureInferenceAnnotationService
from src.infrastructure.logging import StandardLogger, NullLogger
from src.application.services import DomainTelemetryService, MaxOverlapAlignmentService
from src.application.enrichers.segmentation import SentenceSegmentationEnricher
from src.application.enrichers.translation import TranslationEnricher
from src.application.enrichers.annotation import LinguisticAnnotationEnricher


CONTRACTS = [
    (IEventBus, InProcessEventBus),
    (IResultRepository, FileSystemResultRepository),
    (ITranscriptSerializer, JsonTranscriptSerializer),
    (ITranscriber, WhisperTranscriber),
    (ITranscriber, AzureFastTranscriber),
    (IDiarizer, PyannoteDiarizer),
    (IDiarizer, NullDiarizer),
    (IAudioProcessor, FFmpegAudioProcessor),
    (ITranslator, AzureInferenceTranslator),
    (ITranslator, LlamaCppTranslator),
    (ILinguisticAnnotationService, AzureInferenceAnnotationService),
    (ITelemetryService, DomainTelemetryService),
    (ILogger, StandardLogger),
    (ILogger, NullLogger),
    (IAlignmentService, MaxOverlapAlignmentService),
    (IAudioEnricher, SentenceSegmentationEnricher),
    (IAudioEnricher, TranslationEnricher),
    (IAudioEnricher, LinguisticAnnotationEnricher),
]

@pytest.mark.parametrize("interface, implementation", CONTRACTS)
def test_contract_compliance(interface: Type, implementation: Type):
    """
    Verifies that the implementation strictly follows the interface contract.
    Checks for:
    1. Presence of all abstract methods.
    2. Signature match (parameter names, default values, annotations).
    3. Return type annotation match.
    """
    # Get all abstract methods from the interface
    abstract_methods = interface.__abstractmethods__
    # Also check methods that are defined in the interface but not necessarily abstract (if any)
    # But usually we care about abstract ones. 
    # Let's check all methods defined in the interface class.
    
    interface_methods = inspect.getmembers(interface, predicate=inspect.isfunction)
    
    for name, interface_method in interface_methods:
        # We only care about public methods defined in the interface
        if name.startswith("_"):
            continue
            
        assert hasattr(implementation, name), f"{implementation.__name__} missing method '{name}' from {interface.__name__}"
        
        impl_method = getattr(implementation, name)
        
        # Check signature
        if_sig = inspect.signature(interface_method)
        impl_sig = inspect.signature(impl_method)
        
        # Check parameters
        if_params = list(if_sig.parameters.values())
        impl_params = list(impl_sig.parameters.values())
        
        # Skip 'self' which is usually implicit
        if_params = [p for p in if_params if p.name != 'self']
        impl_params = [p for p in impl_params if p.name != 'self']
        
        # 1. Check parameter count
        # Implementations can have optional arguments that are not in the interface (technically),
        # but for strict compliance we might want to check exact match or compatible match.
        # "Strictly follows" suggests exact match of required parameters, and compatible types.
        
        # Let's enforce that the implementation has at least the same parameters in the same order.
        # And they must have matching names and types.
        
        # Verify required parameters match exactly
        if_required = [p for p in if_params if p.default == inspect.Parameter.empty]
        impl_required = [p for p in impl_params if p.default == inspect.Parameter.empty]
        
        assert len(if_required) == len(impl_required), \
            f"Method '{name}' in {implementation.__name__} has different number of required parameters than {interface.__name__}"
            
        for if_p, impl_p in zip(if_required, impl_required):
            assert if_p.name == impl_p.name, \
                f"Method '{name}' parameter mismatch: expected '{if_p.name}', got '{impl_p.name}'"
            assert if_p.annotation == impl_p.annotation, \
                f"Method '{name}' parameter '{if_p.name}' type mismatch: expected {if_p.annotation}, got {impl_p.annotation}"

        # Verify all parameters from interface exist in implementation
        # The implementation might have extra parameters, but they must be optional (have defaults).
        
        if_param_names = [p.name for p in if_params]
        impl_param_names = [p.name for p in impl_params]
        
        for if_p in if_params:
            assert if_p.name in impl_param_names, \
                f"Method '{name}' in {implementation.__name__} missing parameter '{if_p.name}'"
            
            # Find corresponding param in implementation
            impl_p = next(p for p in impl_params if p.name == if_p.name)
            
            # Check annotation
            assert if_p.annotation == impl_p.annotation, \
                f"Method '{name}' parameter '{if_p.name}' type mismatch: expected {if_p.annotation}, got {impl_p.annotation}"
            
            # Check default value if present in interface
            if if_p.default != inspect.Parameter.empty:
                assert impl_p.default == if_p.default, \
                    f"Method '{name}' parameter '{if_p.name}' default value mismatch: expected {if_p.default}, got {impl_p.default}"

        # Check return type
        assert if_sig.return_annotation == impl_sig.return_annotation, \
            f"Method '{name}' return type mismatch: expected {if_sig.return_annotation}, got {impl_sig.return_annotation}"
